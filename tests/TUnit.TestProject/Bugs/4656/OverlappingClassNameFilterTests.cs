using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4656;

/// <summary>
/// Regression test for GitHub issue #4656: TestDiscoveryContext.AllTests is incorrect
/// when filtering tests with overlapping class names (e.g., ABCV vs ABCVC).
/// The bug caused ABCV.B2 to incorrectly appear in AllTests when filtering for
/// ABCD.B2, ABCV.B1, ABCVC.B2, and VPCU.A1000.
/// https://github.com/thomhurst/TUnit/issues/4656
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ABCD
{
    [Test]
    public async Task B1()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test]
    public async Task B2()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class VPCU
{
    [Test, DependsOn<ABCV>(nameof(ABCV.B1))]
    public async Task A1000()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ABCV
{
    [Test]
    public async Task A1()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test, DependsOn(nameof(A1))]
    public async Task B1()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test, DependsOn(nameof(A1))]
    public async Task B2()
    {
        // This test should NOT be included when filtering for ABCVC.B2
        // Bug #4656: substring matching caused this to be incorrectly included
        await Assert.That(true).IsEqualTo(true);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ABCVC
{
    [Test]
    public async Task B0()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test, DependsOn(nameof(B0))]
    public async Task B1()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test, DependsOn(nameof(B0))]
    public async Task B2()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}
