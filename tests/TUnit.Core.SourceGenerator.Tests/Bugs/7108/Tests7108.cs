namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7108 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7108",
        "GenericMethodNonTupleMethodDataSourceTests.cs"),
        _ => Task.CompletedTask);
}
