namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7096 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7096",
        "GenericSameTypeArgumentsTests.cs"),
        _ => Task.CompletedTask);
}
