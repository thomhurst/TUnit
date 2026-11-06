using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Tests.Shared;

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

    // Compile-clean regression tests: parse and compile the generated source through Roslyn,
    // then assert the diagnostic stream contains no error-severity entries. The CS1591 checks
    // above are too narrow to catch emit-syntax bugs (extra punctuation, mis-paired brackets,
    // wrong default-value rendering); these tests close that gap by failing fast on any
    // compiler error in the generated code regardless of its specific diagnostic id.

    [Test]
    public Task AsyncBoolAssertion_GeneratesValidCSharp() => AssertNoCompilationErrors("AsyncBoolAssertion.cs");

    [Test]
    public Task DocumentedMethodAssertion_InheritdocCrefsResolveToSourceMethods()
    {
        var fixturePath = Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "DocumentedMethodAssertion.cs");

        return RunTest(fixturePath, async generatedFiles =>
        {
            // Compile the fixture with its generated code as a GenerateDocumentationFile build does. The
            // test assembly is left out: it contains the fixture too, and its copy would be found first.
            var trees = generatedFiles
                .Prepend(File.ReadAllText(fixturePath))
                .Select(source => CSharpSyntaxTree.ParseText(
                    source,
                    GeneratorTestInput.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose)))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "InheritdocCheck",
                trees,
                ReferencesHelper.References.Where(reference => !string.Equals(
                    Path.GetFileName(reference.FilePath),
                    "TUnit.Assertions.SourceGenerator.Tests.dll",
                    StringComparison.OrdinalIgnoreCase)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var inheritingMethods = new List<string>();
            foreach (var tree in compilation.SyntaxTrees.Skip(1))
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var extensionMethod = semanticModel.GetDeclaredSymbol(declaration)!;
                    if (!extensionMethod.IsExtensionMethod)
                    {
                        continue;
                    }

                    // This is the member's entry in the XML documentation file. A cref the compiler could
                    // not copy verbatim would read "!:...", and malformed XML would not parse.
                    var inheritdoc = XElement.Parse(extensionMethod.GetDocumentationCommentXml()!).Element("inheritdoc");
                    if (inheritdoc is null)
                    {
                        continue;
                    }

                    var cref = inheritdoc.Attribute("cref")!.Value;
                    var sourceMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, compilation);

                    await Assert.That(sourceMethod).IsNotNull();
                    await Assert.That(sourceMethod!.Name).IsEqualTo(extensionMethod.Name);
                    await Assert.That(sourceMethod.GetDocumentationCommentId()).IsEqualTo(cref);
                    await Assert.That(sourceMethod.GetDocumentationCommentXml()).Contains("<summary>");
                    await Assert.That(inheritdoc.Attribute("path")!.Value).IsEqualTo("/*[not(self::returns)]");

                    inheritingMethods.Add(extensionMethod.Name);
                }
            }

            await Assert.That(inheritingMethods.OrderBy(name => name, StringComparer.Ordinal)).IsEquivalentTo(
                new[] { "IsDivisibleByThree", "IsMultipleOf", "IsMultipleOf", "IsSameValueAs", "IsTrimmed", "IsUpperCase" },
                TUnit.Assertions.Enums.CollectionOrdering.Matching);
        });
    }

    private Task AssertNoCS1591(string testDataFile) => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            testDataFile),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();

            var trees = generatedFiles
                .Select(source => CSharpSyntaxTree.ParseText(
                    source,
                    GeneratorTestInput.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose)))
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

    private Task AssertNoCompilationErrors(string testDataFile) => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            testDataFile),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();

            var trees = generatedFiles
                .Select(source => GeneratorTestInput.Parse(source))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "CompileCheck",
                trees,
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // On the net472 leg of CI, the Polyfill assembly's CallerArgumentExpressionAttribute
            // is declared internal, which produces a CS0122 ('inaccessible due to its protection
            // level') false positive when Roslyn compiles the generator's output through the test
            // project's reference set. Filter CS0122 only on net472; the emit shape is still
            // pinned by the `.Net4_7` snapshot files. On modern TFMs the BCL attribute is public,
            // so CS0122 (if it ever appears) would be a genuine signal and is not filtered.
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
                .Where(d => !string.Equals(d.Id, "CS0122", StringComparison.Ordinal))
#endif
                .Select(d => $"{d.Id}: {d.GetMessage()}")
                .ToArray();

            await Assert.That(errors).IsEmpty();
        });
}
