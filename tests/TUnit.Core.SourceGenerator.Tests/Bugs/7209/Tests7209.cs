namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7209 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7209",
        "ClassRowInfersNoTypeArgumentTests.cs"),
        async generatedFiles =>
        {
            // The null method row's C<object> instantiation is emitted beside C<int>, not dropped
            await Assert.That(string.Join("\n", generatedFiles)).Contains("ClassRowInfersNoTypeArgument<object>");
            await Assert.That(string.Join("\n", generatedFiles)).Contains("ClassRowInfersNoTypeArgument<int>");
        });
}
