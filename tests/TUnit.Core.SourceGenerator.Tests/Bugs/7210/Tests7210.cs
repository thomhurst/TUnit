namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7210 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7210",
        "MixedClassSourceRowTests.cs"),
        _ => Task.CompletedTask);
}
