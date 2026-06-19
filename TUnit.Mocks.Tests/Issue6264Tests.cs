using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6264
// Mocking the top of a deep interface hierarchy (each level returns the next via a property)
// failed to compile. Transitive auto-mock discovery was capped at a fixed depth, so the
// deepest generated impl referenced the factory of the next level down — which was never
// generated — producing CS0400 ("the type or namespace name 'IDepthNMockFactory' could not
// be found"). The cap is removed; the full transitive closure of reachable interfaces is now
// generated (bounded by the finite set of distinct types, cycle-safe via the visited set).

#region Test interfaces — deeper than the old depth cap of 3

public interface IDepth1
{
    IDepth2 Inner { get; }
}

public interface IDepth2
{
    IDepth3 Inner { get; }
}

public interface IDepth3
{
    IDepth4 Inner { get; }
}

public interface IDepth4
{
    IDepth5 Inner { get; }
}

public interface IDepth5
{
    IDepth6 Inner { get; }
}

public interface IDepth6
{
    int Value { get; }
}

#endregion

public class Issue6264Tests
{
    [Test]
    public async Task Mocking_Top_Of_Deep_Hierarchy_Compiles()
    {
        // Before the fix this file failed to compile with CS0400 on a generated
        // IDepthN_MockImplFactory referencing an un-generated IDepthN+1MockFactory.
        var mock = IDepth1.Mock();
        await Assert.That(mock).IsNotNull();
    }

    [Test]
    public async Task Auto_Mock_Chains_Through_The_Full_Depth()
    {
        var mock = IDepth1.Mock();

        // Navigate the chain past the old depth-3 cap — each level auto-mocks the next, which is
        // only possible because every level's factory is now generated.
        var leaf = mock.Object.Inner.Inner.Inner.Inner.Inner;

        await Assert.That(leaf).IsNotNull();
        await Assert.That(leaf.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Auto_Mock_At_Depth_Is_Configurable()
    {
        var mock = IDepth1.Mock();

        var leaf = mock.Object.Inner.Inner.Inner.Inner.Inner;
        Mock.Get(leaf).Value.Returns(99);

        await Assert.That(leaf.Value).IsEqualTo(99);
    }
}
