namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7155 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7155",
        "NullAndValueTypeGenericMethodTests.cs"),
        _ => Task.CompletedTask);
}
