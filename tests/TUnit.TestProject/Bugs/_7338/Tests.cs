using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7338;

public sealed class TrackedResource : IDisposable, ITestRegisteredEventReceiver, ILastTestInClassEventReceiver,
    ILastTestInAssemblyEventReceiver, ILastTestInTestSessionEventReceiver
{
    public static TrackedResource? Instance;
    public static int LastInClass;
    public static int LastInAssembly;
    public static int LastInSession;
    public static TestContext? Context;
    public int DisposeCount;
    public int Order => 0;

    public TrackedResource() => Instance = this;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        Context = context.TestContext;
        return default;
    }

    public void Dispose() => Interlocked.Increment(ref DisposeCount);

    public ValueTask OnLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        if (context.ClassType == typeof(DependencySkippedCleanupTests))
        {
            Interlocked.Increment(ref LastInClass);
        }
        return default;
    }

    public ValueTask OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        Interlocked.Increment(ref LastInAssembly);
        return default;
    }

    public ValueTask OnLastTestInTestSession(TestSessionContext context, TestContext testContext)
    {
        Interlocked.Increment(ref LastInSession);
        return default;
    }

}

public sealed class TrackedRowAttribute : UntypedDataSourceGeneratorAttribute
{
    public static int OnDisposeCount;

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += (_, _) =>
            {
                Interlocked.Increment(ref OnDisposeCount);
                return default;
            };
            return [new TrackedResource()];
        };
    }
}

public class DependencySkippedCleanupTests
{
    public static int SetupCount;
    public static int AfterClassCount;

    [Before(Test)]
    public void Setup() => Interlocked.Increment(ref SetupCount);

    [Test]
    public void Fails() => throw new InvalidOperationException("Intentional prerequisite failure");

    [Test]
    [DependsOn(nameof(Fails))]
    [TrackedRow]
    public void FinalTest(TrackedResource resource) => throw new InvalidOperationException("Must never run");

    [After(Class)]
    public static void AfterClass() => Interlocked.Increment(ref AfterClassCount);
}

public class CleanupObserverTests
{
    [Test]
    [DependsOn(typeof(DependencySkippedCleanupTests), ProceedOnFailure = true)]
    public async Task DependencySkipCompletesCleanup()
    {
        await Assert.That(TrackedResource.Instance).IsNotNull();
        await Assert.That(TrackedResource.Instance!.DisposeCount).IsEqualTo(1);
        await Assert.That(TrackedRowAttribute.OnDisposeCount).IsEqualTo(1);
        await Assert.That(TrackedResource.LastInClass).IsEqualTo(1);
        await Assert.That(DependencySkippedCleanupTests.AfterClassCount).IsEqualTo(1);
        await Assert.That(DependencySkippedCleanupTests.SetupCount).IsEqualTo(1);
        await Assert.That(TrackedResource.Context!.Execution.Result!.State).IsEqualTo(TestState.Skipped);
    }
}

// Also run with only DependencySkippedCleanupTests selected, making the skipped test the
// final outstanding test in the assembly and session, rather than just in its class.
public class CleanupHooks
{
    private static int _afterAssemblyCount;

    [After(Assembly)]
    public static void AfterAssembly()
    {
        if (TrackedResource.Instance is not null)
        {
            Interlocked.Increment(ref _afterAssemblyCount);
        }
    }

    [After(TestSession)]
    public static async Task VerifyCompletion()
    {
        // Discovery constructs data rows even when this regression is filtered out.
        // Only the dedicated child runs opt in to these session-wide assertions.
        if (Environment.GetEnvironmentVariable("TUNIT_7338_COMPLETION_PATH") is not { } completionPath)
        {
            return;
        }

        await Assert.That(TrackedResource.Instance).IsNotNull();
        await Assert.That(TrackedResource.Instance.DisposeCount).IsEqualTo(1);
        await Assert.That(TrackedRowAttribute.OnDisposeCount).IsEqualTo(1);
        await Assert.That(DependencySkippedCleanupTests.AfterClassCount).IsEqualTo(1);
        await Assert.That(TrackedResource.LastInClass).IsEqualTo(1);
        await Assert.That(_afterAssemblyCount).IsEqualTo(1);
        await Assert.That(TrackedResource.LastInAssembly).IsEqualTo(1);
        await Assert.That(TrackedResource.LastInSession).IsEqualTo(1);

        await File.WriteAllTextAsync(completionPath, "completed");
    }
}
