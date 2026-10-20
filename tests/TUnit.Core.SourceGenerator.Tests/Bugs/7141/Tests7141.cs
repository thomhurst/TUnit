namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7141 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7141",
        "NullArgumentsRowOnGenericClassTests.cs"),
        async generatedFiles =>
        {
            // The null row's C<object> instantiation is emitted, not dropped
            await Assert.That(string.Join("\n", generatedFiles)).Contains("NullRowOnGenericClass<object>");
        });
}
