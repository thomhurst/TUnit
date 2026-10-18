namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7106 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7106",
        "GenericClassRowsTests.cs"),
        _ => Task.CompletedTask);
}
