using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4584;

/// <summary>
/// Tests that verify dependencies are available in After(TestDiscovery) hooks.
/// This is the fix for issue #4584 where dependencies were not populated when the hook ran.
/// </summary>
public class DependenciesInAfterTestDiscoveryHookTests
{
    /// <summary>
    /// Stores the dependencies found for focused tests during the AfterTestDiscovery hook.
    /// </summary>
    public static ConcurrentDictionary<string, IReadOnlyList<TestDetails>> CapturedDependenciesInHook { get; } = new();

    /// <summary>
    /// Flag to track if any focused tests were found during discovery.
    /// </summary>
    public static bool FocusedTestsFound { get; set; }

    /// <summary>
    /// Stores the number of dependencies found for focused tests during the hook.
    /// </summary>
    public static int DependencyCountInHook { get; set; }

    [After(TestDiscovery)]
    public static void CaptureTestDependencies(TestDiscoveryContext context)
    {
        // Find tests that have the FocusTestAttribute (simulating the user's Focus scenario)
        var focusedTests = context.AllTests
            .Where(t => t.Metadata.TestDetails.Attributes.HasAttribute<FocusTestAttribute>())
            .ToArray();

        FocusedTestsFound = focusedTests.Length > 0;

        // Capture dependencies for each focused test
        foreach (var test in focusedTests)
        {
            var dependencies = test.Dependencies.DependsOn;
            CapturedDependenciesInHook[test.Metadata.TestDetails.TestId] = dependencies.ToList();
            DependencyCountInHook += dependencies.Count;
        }
    }
}

/// <summary>
/// A marker attribute to simulate the [Focus] attribute from the user's scenario.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FocusTestAttribute : Attribute;

/// <summary>
/// A skip attribute that uses dependencies from focused tests captured in AfterTestDiscovery hook.
/// This closely mirrors the user's FocusAssemblyAttribute pattern.
/// </summary>
public class FocusSkipAttribute4584 : SkipAttribute
{
    private static TestContext[] _focusedTests = [];

    public FocusSkipAttribute4584() : base("This test is not focused") { }

    [After(TestDiscovery)]
    public static void AfterTestDiscovery(TestDiscoveryContext context)
    {
        _focusedTests = context.AllTests
            .Where(t => t.Metadata.TestDetails.Attributes.HasAttribute<SkipFocusTestAttribute>())
            .ToArray();
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        // Run all tests if no tests are focused
        if (_focusedTests.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Run because it's a dependency of a focused test
        if (IsDependencyOfFocusedTest(context.TestContext))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(!HasFocusAttribute(context.TestContext));
    }

    private static bool HasFocusAttribute(TestContext test)
        => test.Metadata.TestDetails.Attributes.HasAttribute<SkipFocusTestAttribute>();

    private static bool IsDependencyOfFocusedTest(TestContext test)
        => _focusedTests
            .SelectMany(x => x.Dependencies.DependsOn.Select(y => y.TestId))
            .Contains(test.Metadata.TestDetails.TestId);
}

/// <summary>
/// Separate focus attribute for the skip test scenario.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SkipFocusTestAttribute : Attribute;

#region Basic Dependency Tests

/// <summary>
/// A dependency target class - this should be captured as a dependency in the hook.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DependencyTarget4584
{
    public static bool WasExecuted { get; set; }

    [Test]
    public async Task DependencyTest()
    {
        WasExecuted = true;
        await Task.CompletedTask;
    }
}

/// <summary>
/// A "focused" test class that depends on DependencyTarget4584.
/// The After(TestDiscovery) hook should see the dependencies when this test is processed.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
[DependsOn(typeof(DependencyTarget4584))]
public class FocusedTestWithClassDependency4584
{
    [Test]
    public async Task VerifyDependenciesWereCapturedInHook()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the hook found this as a focused test
        await Assert.That(DependenciesInAfterTestDiscoveryHookTests.FocusedTestsFound).IsTrue();

        // Verify the hook captured our dependencies
        await Assert.That(DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook.ContainsKey(testId)).IsTrue();

        // Verify the dependency was available in the hook
        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
        await Assert.That(capturedDeps[0].ClassType).IsEqualTo(typeof(DependencyTarget4584));

        // Verify the dependency ran before us
        await Assert.That(DependencyTarget4584.WasExecuted).IsTrue();
    }
}

/// <summary>
/// Another focused test, but with a method-level dependency.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
public class FocusedTestWithMethodDependency4584
{
    [Test]
    [DependsOn(typeof(DependencyTarget4584))]
    public async Task VerifyMethodDependenciesWereCapturedInHook()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the hook captured our dependencies
        await Assert.That(DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook.ContainsKey(testId)).IsTrue();

        // Verify the dependency was available in the hook
        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
    }
}

#endregion

#region Transitive Dependency Tests

/// <summary>
/// Tests transitive dependencies in the AfterTestDiscovery hook.
/// This is a transitive dependency target.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TransitiveDependencyTarget4584
{
    public static bool WasExecuted { get; set; }

    [Test]
    public async Task TransitiveTarget()
    {
        WasExecuted = true;
        await Task.CompletedTask;
    }
}

/// <summary>
/// An intermediate dependency that depends on TransitiveDependencyTarget4584.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[DependsOn(typeof(TransitiveDependencyTarget4584))]
public class IntermediateDependency4584
{
    public static bool WasExecuted { get; set; }

    [Test]
    public async Task IntermediateTest()
    {
        WasExecuted = true;
        await Assert.That(TransitiveDependencyTarget4584.WasExecuted).IsTrue();
    }
}

/// <summary>
/// A focused test with transitive dependencies.
/// The hook should see BOTH the direct dependency (IntermediateDependency4584)
/// AND the transitive dependency (TransitiveDependencyTarget4584).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
[DependsOn(typeof(IntermediateDependency4584))]
public class FocusedTestWithTransitiveDependencies4584
{
    [Test]
    public async Task VerifyTransitiveDependenciesWereCapturedInHook()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the hook captured our dependencies
        await Assert.That(DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook.ContainsKey(testId)).IsTrue();

        // Verify BOTH direct and transitive dependencies were available in the hook
        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];
        await Assert.That(capturedDeps).HasCount().EqualTo(2);

        var depTypes = capturedDeps.Select(d => d.ClassType).ToList();
        await Assert.That(depTypes).Contains(typeof(IntermediateDependency4584));
        await Assert.That(depTypes).Contains(typeof(TransitiveDependencyTarget4584));

        // Verify both dependencies ran before us
        await Assert.That(IntermediateDependency4584.WasExecuted).IsTrue();
        await Assert.That(TransitiveDependencyTarget4584.WasExecuted).IsTrue();
    }
}

#endregion

#region Skip Attribute Tests - Mirrors User's FocusAssemblyAttribute Pattern

/// <summary>
/// A shared dependency that multiple focused tests depend on.
/// This test should NOT be skipped because it's a dependency of focused tests.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusSkipAttribute4584]
public class SharedDependencyForSkipTest4584
{
    public static int ExecutionCount { get; set; }

    [Test]
    public void SharedDependencyTest()
    {
        ExecutionCount++;
    }
}

/// <summary>
/// First focused test that depends on SharedDependencyForSkipTest4584.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusSkipAttribute4584]
[SkipFocusTest]
[DependsOn(typeof(SharedDependencyForSkipTest4584))]
public class FirstFocusedTestForSkip4584
{
    [Test]
    public async Task VerifyDependencyRan()
    {
        // The shared dependency should have run because we depend on it
        await Assert.That(SharedDependencyForSkipTest4584.ExecutionCount).IsGreaterThanOrEqualTo(1);
    }
}

/// <summary>
/// Second focused test that also depends on SharedDependencyForSkipTest4584.
/// Tests that multiple focused tests can share the same dependency.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusSkipAttribute4584]
[SkipFocusTest]
[DependsOn(typeof(SharedDependencyForSkipTest4584))]
public class SecondFocusedTestForSkip4584
{
    [Test]
    public async Task VerifyDependencyRan()
    {
        // The shared dependency should have run
        await Assert.That(SharedDependencyForSkipTest4584.ExecutionCount).IsGreaterThanOrEqualTo(1);
    }
}

// Note: A test verifying skip behavior would need ExpectedResult.Skipped which doesn't exist.
// The skip functionality is implicitly tested by the other tests - if dependencies weren't
// available in the hook, the SharedDependencyForSkipTest4584 would be incorrectly skipped
// and FirstFocusedTestForSkip4584/SecondFocusedTestForSkip4584 would fail.

#endregion

#region Multiple Focused Tests with Shared Transitive Dependencies

/// <summary>
/// Root dependency for testing multiple focused tests with shared transitive dependencies.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RootDependency4584
{
    public static bool WasExecuted { get; set; }

    [Test]
    public void RootTest()
    {
        WasExecuted = true;
    }
}

/// <summary>
/// Middle layer dependency that depends on root.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[DependsOn(typeof(RootDependency4584))]
public class MiddleDependency4584
{
    public static bool WasExecuted { get; set; }

    [Test]
    public async Task MiddleTest()
    {
        WasExecuted = true;
        await Assert.That(RootDependency4584.WasExecuted).IsTrue();
    }
}

/// <summary>
/// First focused test depending on middle layer.
/// Should see both middle and root as dependencies.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
[DependsOn(typeof(MiddleDependency4584))]
public class FocusedWithMiddleDependency4584
{
    [Test]
    public async Task VerifyAllDependenciesVisible()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;
        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];

        // Should see both middle and root
        await Assert.That(capturedDeps).HasCount().EqualTo(2);

        var depTypes = capturedDeps.Select(d => d.ClassType).ToList();
        await Assert.That(depTypes).Contains(typeof(MiddleDependency4584));
        await Assert.That(depTypes).Contains(typeof(RootDependency4584));
    }
}

/// <summary>
/// Second focused test directly depending on root.
/// Should see only root as dependency.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
[DependsOn(typeof(RootDependency4584))]
public class FocusedWithRootDependency4584
{
    [Test]
    public async Task VerifyOnlyRootVisible()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;
        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];

        // Should see only root
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
        await Assert.That(capturedDeps[0].ClassType).IsEqualTo(typeof(RootDependency4584));
    }
}

#endregion

#region No Dependencies Test

/// <summary>
/// A focused test with no dependencies.
/// Should have empty dependencies list in the hook.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[FocusTest]
public class FocusedTestWithNoDependencies4584
{
    [Test]
    public async Task VerifyEmptyDependenciesInHook()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Should be captured even with no dependencies
        await Assert.That(DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook.ContainsKey(testId)).IsTrue();

        var capturedDeps = DependenciesInAfterTestDiscoveryHookTests.CapturedDependenciesInHook[testId];
        await Assert.That(capturedDeps).IsEmpty();
    }
}

#endregion
