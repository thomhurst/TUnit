#pragma warning disable TUnit0004 // No method found - intentionally testing missing dependencies

namespace TUnit.UnitTests;

/// <summary>
/// Tests to verify that dependency resolution failures don't abort the entire test run
/// </summary>
public class DependencyResolutionFailureTests
{
    [Test]
    public async Task TestWithValidDependency()
    {
        // This test should run successfully
        await Assert.That(1 + 1).IsEqualTo(2);
    }

    [Test]
    [DependsOn("NonExistentTest")]
    public async Task TestWithMissingDependency()
    {
        // This test should be marked as failed due to missing dependency
        // but should not prevent other tests from running
        await Assert.That(true).IsTrue();
    }

    [Test]
    [DependsOn(nameof(TestWithMissingDependency))]
    public async Task TestDependingOnFailedDependencyResolution()
    {
        // This test depends on a test that failed dependency resolution
        // It should also fail or be skipped
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task AnotherIndependentTest()
    {
        // This test has no dependencies and should run normally
        await Assert.That("test").IsNotNull();
    }

    [Test]
    [DependsOn("TestMethod", [typeof(string), typeof(int)])]
    public async Task TestWithMissingOverloadDependency()
    {
        // This test depends on a method overload that doesn't exist
        await Assert.That(true).IsTrue();
    }
}
