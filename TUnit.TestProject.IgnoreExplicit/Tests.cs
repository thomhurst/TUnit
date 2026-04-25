namespace TUnit.TestProject.IgnoreExplicit;

// Standalone fixture used by TUnit.Engine.Tests.ExplicitTests to exercise the
// no-filter (NopFilter) code path with --ignore-explicit. Running this project
// without --treenode-filter is safe because every test here is non-failing.
public class Tests
{
    [Test]
    public Task NormalTest() => Task.CompletedTask;

    [Test, Explicit]
    public Task ExplicitTest() => Task.CompletedTask;
}

[Explicit]
public class ExplicitClass
{
    [Test]
    public Task TestA() => Task.CompletedTask;

    [Test]
    public Task TestB() => Task.CompletedTask;
}
