namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7118 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7118",
        "MixedTypeRowsTests.cs"),
        _ => Task.CompletedTask);
}
