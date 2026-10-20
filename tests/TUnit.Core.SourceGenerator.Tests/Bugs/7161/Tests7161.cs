namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7161 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7161",
        "ClassDataForMethodRowInstantiationTests.cs"),
        _ => Task.CompletedTask);
}
