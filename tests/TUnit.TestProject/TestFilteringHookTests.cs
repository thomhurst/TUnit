using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Global TestFiltering/TestDiscovery hooks. They run for every TestProject run, so every
// predicate targets only the test classes in this file.
public static class TestFilteringHooks
{
    public static int BeforeEveryCalls;
    public static int AfterEveryCalls;
    public static bool AfterHookSawExcludedTest;
    public static bool AfterHookSawReincludedDependency;
    public static bool AfterHookMutationThrew;

    [After(TestDiscovery)]
    public static void ExcludeAtDiscovery(TestDiscoveryContext context)
    {
        context.ExcludeTests(t => Is<TestFilteringHookTests>(t, nameof(TestFilteringHookTests.ExcludedAtDiscovery)));
    }

    [Before(TestFiltering)]
    public static void ExcludeAndSkip(TestFilteringContext context)
    {
        context.ExcludeTests(t => Is<TestFilteringHookTests>(t, nameof(TestFilteringHookTests.ExcludedByHook))
                                  || Is<TestFilteringDependencyTests>(t, nameof(TestFilteringDependencyTests.ExcludedDependency))
                                  || Is<TestFilteringLookupTests>(t, nameof(TestFilteringLookupTests.ExcludedSibling)));

        context.SkipTests(t => Is<TestFilteringHookTests>(t, nameof(TestFilteringHookTests.SkippedByHook))
                               || Is<TestFilteringDependencyTests>(t, nameof(TestFilteringDependencyTests.SkippedDependency)),
            "Skipped by a TestFiltering hook");
    }

    [BeforeEvery(TestFiltering)]
    public static void CountBeforeEvery()
    {
        Interlocked.Increment(ref BeforeEveryCalls);
    }

    [After(TestFiltering)]
    public static void InspectFinalSet(TestFilteringContext context)
    {
        AfterHookSawExcludedTest = context.AllTests.Any(t => Is<TestFilteringHookTests>(t, nameof(TestFilteringHookTests.ExcludedByHook)));
        AfterHookSawReincludedDependency = context.AllTests.Any(t => Is<TestFilteringDependencyTests>(t, nameof(TestFilteringDependencyTests.ExcludedDependency)));

        try
        {
            context.ExcludeTests(_ => false);
        }
        catch (InvalidOperationException)
        {
            AfterHookMutationThrew = true;
        }
    }

    [AfterEvery(TestFiltering)]
    public static async Task CountAfterEvery(TestFilteringContext context, CancellationToken cancellationToken)
    {
        await Task.Yield();
        Interlocked.Increment(ref AfterEveryCalls);
    }

    private static bool Is<T>(TestContext test, string testName)
        => test.Metadata.TestDetails.ClassType == typeof(T) && test.Metadata.TestDetails.TestName == testName;
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringHookTests
{
    [Test]
    public void ExcludedByHook()
    {
        throw new InvalidOperationException("Excluded by a [Before(TestFiltering)] hook, so it must never run.");
    }

    [Test]
    public void ExcludedAtDiscovery()
    {
        throw new InvalidOperationException("Excluded by an [After(TestDiscovery)] hook, so it must never run.");
    }

    [Test]
    public void SkippedByHook()
    {
        throw new InvalidOperationException("Skipped by a [Before(TestFiltering)] hook, so it must never run.");
    }

    [Test]
    public async Task FilteringHooksRan()
    {
        await Assert.That(TestFilteringHooks.BeforeEveryCalls).IsGreaterThan(0);
        await Assert.That(TestFilteringHooks.AfterEveryCalls).IsGreaterThan(0);
        await Assert.That(TestFilteringHooks.AfterHookSawExcludedTest).IsFalse();
        await Assert.That(TestFilteringHooks.AfterHookMutationThrew).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringDependencyTests
{
    // Excluded by the hook, but re-included because DependsOnExcludedTest depends on it.
    [Test]
    public void ExcludedDependency()
    {
    }

    [Test, DependsOn(nameof(ExcludedDependency))]
    public async Task DependsOnExcludedTest()
    {
        await Assert.That(TestFilteringHooks.AfterHookSawReincludedDependency).IsTrue();
    }

    [Test]
    public void SkippedDependency()
    {
        throw new InvalidOperationException("Skipped by a [Before(TestFiltering)] hook, so it must never run.");
    }

    // Skipped because its dependency was skipped.
    [Test, DependsOn(nameof(SkippedDependency))]
    public void DependsOnSkippedTest()
    {
        throw new InvalidOperationException("Its dependency was skipped, so it must never run.");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringLookupTests
{
    [Test]
    public void ExcludedSibling()
    {
        throw new InvalidOperationException("Excluded by a [Before(TestFiltering)] hook, so it must never run.");
    }

    [Test]
    public void FinishedSibling()
    {
    }

    [Test, DependsOn(nameof(FinishedSibling))]
    public async Task Lookups_Ignore_Hook_Excluded_Tests()
    {
        var dependencies = TestContext.Current!.Dependencies;

        // An excluded test never gets a result, so finding it would throw "Cannot get unfinished tests".
        await Assert.That(dependencies.GetTests(nameof(ExcludedSibling))).IsEmpty();
        await Assert.That(dependencies.GetTests(nameof(FinishedSibling))).Count().IsEqualTo(1);
    }
}
