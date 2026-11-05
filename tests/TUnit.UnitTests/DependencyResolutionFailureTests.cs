#pragma warning disable TUnit0004 // No method found - intentionally testing missing dependencies

namespace TUnit.UnitTests;

/// <summary>
/// Tests to verify that dependency resolution failures don't abort the entire test run
/// </summary>
public class DependencyResolutionFailureTests
{
    [Test]
    public Task TestWithValidDependency()
    {
        // This test should run successfully
        return Task.CompletedTask;
    }

    [Test]
    [DependsOn("NonExistentTest")]
    public Task TestWithMissingDependency()
    {
        // This test should be marked as failed due to missing dependency
        // but should not prevent other tests from running
        return Task.CompletedTask;
    }

    [Test]
    [DependsOn(nameof(TestWithMissingDependency))]
    public Task TestDependingOnFailedDependencyResolution()
    {
        // This test depends on a test that failed dependency resolution
        // It should also fail or be skipped
        return Task.CompletedTask;
    }

    [Test]
    public async Task AnotherIndependentTest()
    {
        // This test has no dependencies and should run normally
        await Assert.That("test").IsNotNull();
    }

    [Test]
    [DependsOn("TestMethod", [typeof(string), typeof(int)])]
    public Task TestWithMissingOverloadDependency()
    {
        // This test depends on a method overload that doesn't exist
        return Task.CompletedTask;
    }
}
