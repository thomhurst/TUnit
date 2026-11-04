namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedGenericArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7490", "Tests.cs"),
        async generatedFiles => await Assert.That(string.Join("\n", generatedFiles))
            .Contains("InheritedGenericTestNullArguments<object>")
            .And.Contains("InheritedSourceNullArguments<string>"));
}
