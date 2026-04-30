using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class XmlDocsRegressionTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task BoolMethodAssertion_ProducesNoCS1591() => AssertNoCS1591("BoolMethodAssertion.cs");

    [Test]
    public Task AsyncBoolAssertion_ProducesNoCS1591() => AssertNoCS1591("AsyncBoolAssertion.cs");

    [Test]
    public Task AssertionResultOfTMethodAssertion_ProducesNoCS1591() => AssertNoCS1591("AssertionResultOfTMethodAssertion.cs");

    [Test]
    public Task FileScopedClassAssertion_ProducesNoCS1591() => AssertNoCS1591("FileScopedClassAssertion.cs");

    [Test]
    public Task MethodWithComparableConstraint_ProducesNoCS1591() => AssertNoCS1591("MethodWithComparableConstraint.cs");

    private Task AssertNoCS1591(string testDataFile) => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            testDataFile),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();

            var trees = generatedFiles
                .Select(source => CSharpSyntaxTree.ParseText(
                    source,
                    CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose)))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "XmlDocsRegressionCheck",
                trees,
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: 4));

            var cs1591 = compilation.GetDiagnostics()
                .Where(d => string.Equals(d.Id, "CS1591", StringComparison.Ordinal))
                .ToArray();

            await Assert.That(cs1591).IsEmpty();
        });
}
