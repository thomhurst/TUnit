namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests6657 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "6657",
        "Tests.cs"),
        _ => Task.CompletedTask);
}
