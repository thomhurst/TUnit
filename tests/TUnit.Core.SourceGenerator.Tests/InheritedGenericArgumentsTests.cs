namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedGenericArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7490", "Tests.cs"),
        async generatedFiles => await Assert.That(string.Join("\n", generatedFiles))
            .Contains("InheritedGenericTestNullArguments<object>")
            .And.Contains("InheritedSourceNullArguments<string>")
            .And.Contains("InheritedExternalSource<string>")
            .And.Contains("InheritedConstrainedExternalSource<string>")
            .And.Contains("InheritedPrivateRowSource<object>")
            .And.DoesNotContain("InheritedExternalSource<global::System.IComparable>")
            .And.DoesNotContain("GenericConstraintViolationEntry")
            .And.DoesNotContain("PrivateRowSourceBase.PrivateRow"));
}
