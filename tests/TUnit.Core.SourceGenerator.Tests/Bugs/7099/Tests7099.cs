namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7099 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7099",
        "Tests.cs"),
        async generatedFiles =>
        {
            foreach (var file in generatedFiles)
            {
                await Assert.That(file).DoesNotContain("typeof(T[])");
                await Assert.That(file).DoesNotContain("Cast<T[]>");
            }
        });
}
