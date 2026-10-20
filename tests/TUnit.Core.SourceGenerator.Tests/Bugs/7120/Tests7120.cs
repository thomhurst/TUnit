namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7120 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7120",
        "NullArgumentsGenericMethodTests.cs"),
        _ => Task.CompletedTask);
}
