using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[MicrosoftDependencyInjectionDataSource]
[UnconditionalSuppressMessage("Usage", "TUnit0042:Global hooks should not be mixed with test classes to avoid confusion. Place them in their own class.")]
public class SkippedScopeLifecycleTests(MEDIClass service)
{
    public static IEnumerable<string> NoRows() => [];

    [Test]
    [MethodDataSource(nameof(NoRows), SkipIfEmpty = true)]
    public void EmptySource(string value)
    {
        _ = service;
        throw new InvalidOperationException("Must be skipped");
    }

    [After(TestSession)]
    public static async Task VerifyScopeCleanup(TestSessionContext session)
    {
        var tests = session.TestClasses
            .Where(x => x.ClassType == typeof(SkippedScopeLifecycleTests))
            .SelectMany(x => x.Tests)
            .Where(x => x.Execution.Result != null)
            .ToArray();

        foreach (var test in tests)
        {
            var argument = test.Metadata.TestDetails.TestClassArguments.OfType<MEDIClass>().Single();
            await Assert.That(argument.DisposeCount).IsEqualTo(1);
        }
    }
}

[EngineTest(ExpectedResult.Failure)]
[MicrosoftDependencyInjectionDataSource]
[UnconditionalSuppressMessage("Usage", "TUnit0042:Global hooks should not be mixed with test classes to avoid confusion. Place them in their own class.")]
public class FailedScopeLifecycleTests
{
    private static MEDIClass? _service;

    public FailedScopeLifecycleTests(MEDIClass service)
    {
        _service = service;
        throw new InvalidOperationException("Intentional constructor failure");
    }

    public IEnumerable<string> Rows() => ["row"];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void FailedInstance(string value) => throw new InvalidOperationException("Must not execute");

    [After(TestSession)]
    public static async Task VerifyScopeCleanup()
    {
        if (_service is { } service)
        {
            await Assert.That(service.DisposeCount).IsEqualTo(1);
        }
    }
}
