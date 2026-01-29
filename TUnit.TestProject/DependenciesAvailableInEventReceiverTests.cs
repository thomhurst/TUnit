using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Attribute that captures dependencies during test registration.
/// This verifies that dependencies are available in ITestRegisteredEventReceiver.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CaptureDependenciesAttribute : Attribute, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Stores the captured dependencies for each test by test ID.
    /// </summary>
    public static ConcurrentDictionary<string, IReadOnlyList<TestDetails>> CapturedDependencies { get; } = new();

    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        // Capture the dependencies available at registration time
        var dependencies = context.TestContext.Dependencies.DependsOn;
        var testId = context.TestContext.Metadata.TestDetails.TestId;

        // Store a copy of the dependencies list
        CapturedDependencies[testId] = dependencies.ToList();

        return default;
    }
}

/// <summary>
/// Tests verifying that dependencies are available in ITestRegisteredEventReceiver.
/// This is the key feature request from issue #4584.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[CaptureDependencies]
public class DependenciesAvailableInEventReceiverTests
{
    [Test]
    public async Task IndependentTest()
    {
        // This test has no dependencies
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the attribute captured this test (even with no dependencies)
        await Assert.That(CaptureDependenciesAttribute.CapturedDependencies.ContainsKey(testId)).IsTrue();

        // Verify no dependencies were captured
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];
        await Assert.That(capturedDeps).IsEmpty();
    }

    [Test]
    [DependsOn(nameof(IndependentTest))]
    public async Task DependentTest_HasDependenciesAtRegistration()
    {
        // This test depends on IndependentTest
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the attribute captured this test
        await Assert.That(CaptureDependenciesAttribute.CapturedDependencies.ContainsKey(testId)).IsTrue();

        // Verify the dependency was captured at registration time
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
        await Assert.That(capturedDeps[0].TestName).IsEqualTo(nameof(IndependentTest));
    }
}

/// <summary>
/// A simple dependency target class for cross-class dependency tests.
/// </summary>
public class DependencyTargetClass
{
    public static bool WasExecuted { get; set; }

    [Test]
    public async Task TargetTest()
    {
        WasExecuted = true;
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tests verifying cross-class dependencies are available in ITestRegisteredEventReceiver.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[CaptureDependencies]
[DependsOn(typeof(DependencyTargetClass))]
public class CrossClassDependenciesInEventReceiverTests
{
    [Test]
    public async Task Test_HasCrossClassDependenciesAtRegistration()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;

        // Verify the attribute captured this test
        await Assert.That(CaptureDependenciesAttribute.CapturedDependencies.ContainsKey(testId)).IsTrue();

        // Verify the cross-class dependency was captured at registration time
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
        await Assert.That(capturedDeps[0].ClassType).IsEqualTo(typeof(DependencyTargetClass));
        await Assert.That(capturedDeps[0].TestName).IsEqualTo(nameof(DependencyTargetClass.TargetTest));

        // Also verify the dependency actually ran first
        await Assert.That(DependencyTargetClass.WasExecuted).IsTrue();
    }
}

/// <summary>
/// Tests verifying transitive dependencies are available in ITestRegisteredEventReceiver.
/// A -> B -> C means A should see both B and C in its dependencies.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[CaptureDependencies]
public class TransitiveDependenciesInEventReceiverTests
{
    [Test]
    public async Task TestC_NoDependencies()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];
        await Assert.That(capturedDeps).IsEmpty();
    }

    [Test]
    [DependsOn(nameof(TestC_NoDependencies))]
    public async Task TestB_DependsOnC()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];

        // B depends directly on C
        await Assert.That(capturedDeps).HasCount().EqualTo(1);
        await Assert.That(capturedDeps[0].TestName).IsEqualTo(nameof(TestC_NoDependencies));
    }

    [Test]
    [DependsOn(nameof(TestB_DependsOnC))]
    public async Task TestA_DependsOnB_ShouldSeeTransitiveDependencies()
    {
        var testId = TestContext.Current!.Metadata.TestDetails.TestId;
        var capturedDeps = CaptureDependenciesAttribute.CapturedDependencies[testId];

        // A depends on B, but should also see C as a transitive dependency
        await Assert.That(capturedDeps).HasCount().EqualTo(2);

        var depNames = capturedDeps.Select(d => d.TestName).ToList();
        await Assert.That(depNames).Contains(nameof(TestB_DependsOnC));
        await Assert.That(depNames).Contains(nameof(TestC_NoDependencies));
    }
}
