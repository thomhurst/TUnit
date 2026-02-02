using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests for transitive dependency handling when dependencies fail.
/// Reproduces GitHub issue #4643: Tests not skipped when transitive dependencies fail
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class TransitiveDependenciesTests
{
    [Test]
    public async Task Test1()
    {
        // This test fails intentionally
        await Assert.That(true).IsEqualTo(false);
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        // Test2 depends on Test1, which fails
        // Test2 should be skipped
        await Assert.That(true).IsEqualTo(false);
    }

    [Test, DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        // Test3 depends on Test2, which depends on Test1
        // When Test1 fails, Test2 is skipped
        // Therefore Test3 should also be skipped (transitive dependency)
        await Assert.That(true).IsEqualTo(false);
    }
}
