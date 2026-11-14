using System.Xml.Linq;
using System.Xml.XPath;
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
            // Compile the fixture with its generated code as a GenerateDocumentationFile build does.
            var trees = generatedFiles
                .Prepend(File.ReadAllText(fixturePath))
                .Select(source => CSharpSyntaxTree.ParseText(
                    source,
                    GeneratorTestInput.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose)))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "InheritdocCheck",
                trees,
                ReferencesHelper.WithoutTestAssembly(),
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
                    await Assert.That(inheritdoc.Attribute("path")!.Value).IsEqualTo(
                        $"/*[not(self::returns) and not(self::param[@name='{((IMethodSymbol)sourceMethod).Parameters[0].Name}'])]");

                    inheritingMethods.Add(extensionMethod.Name);
                }
            }

            await Assert.That(inheritingMethods.OrderBy(name => name, StringComparer.Ordinal)).IsEquivalentTo(
                new[] { "IsDivisibleByThree", "IsMultipleOf", "IsMultipleOf", "IsSameValueAs", "IsTrimmed", "IsUpperCase" },
                TUnit.Assertions.Enums.CollectionOrdering.Matching);
        });
    }

    [Test]
    public Task DocumentedMethodAssertion_InheritdocCrefsResolveFromPackage()
    {
        var fixturePath = Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "DocumentedMethodAssertion.cs");

        return RunTest(fixturePath, async generatedFiles =>
        {
            // Build the fixture and its generated code as a library with its XML documentation file, then
            // reference both from a second compilation, which sees the library as a package consumer does.
            var trees = generatedFiles
                .Prepend(File.ReadAllText(fixturePath))
#if NETFRAMEWORK
                .Append(CompileChecker.NetFrameworkPolyfills)
#endif
                .Select((source, index) => CSharpSyntaxTree.ParseText(
                    source,
                    GeneratorTestInput.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose),
                    path: $"Source{index}.cs"))
                .ToArray();

            var library = CSharpCompilation.Create(
                "DocumentedLibrary",
                trees,
                ReferencesHelper.WithoutTestAssembly(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var image = new MemoryStream();
            using var documentation = new MemoryStream();
            var emitResult = library.Emit(image, xmlDocumentationStream: documentation);
            await Assert.That(emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())).IsEmpty();

            var consumer = CSharpCompilation.Create(
                "PackageConsumer",
                references: ReferencesHelper.WithoutTestAssembly().Append(MetadataReference.CreateFromImage(
                    image.ToArray(),
                    documentation: XmlDocumentationProvider.CreateFromBytes(documentation.ToArray()))),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            documentation.Position = 0;
            var members = XDocument.Load(documentation).Descendants("member").ToArray();

            var inheritingMethods = new List<string>();
            foreach (var member in members)
            {
                var inheritdoc = member.Element("inheritdoc");
                if (inheritdoc is null)
                {
                    continue;
                }

                // As an IDE does: find the cref among the members the package exposes, read that member's
                // docs from the package's XML file, and apply the path.
                var extensionMethod = (IMethodSymbol?)DocumentationCommentId.GetFirstSymbolForDeclarationId(
                    member.Attribute("name")!.Value, consumer);
                var sourceMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId(
                    inheritdoc.Attribute("cref")!.Value, consumer);

                await Assert.That(extensionMethod).IsNotNull();
                await Assert.That(sourceMethod).IsNotNull();
                await Assert.That(sourceMethod!.ContainingAssembly.Name).IsEqualTo("DocumentedLibrary");

                var inherited = XDocument.Parse(sourceMethod.GetDocumentationCommentXml()!)
                    .XPathSelectElements("/*" + inheritdoc.Attribute("path")!.Value)
                    .ToArray();
                var parameterNames = extensionMethod!.Parameters.Select(parameter => parameter.Name).ToArray();

                await Assert.That(inherited.Select(element => element.Name.LocalName)).Contains("summary");
                await Assert.That(inherited.Select(element => element.Name.LocalName)).DoesNotContain("returns");
                foreach (var param in inherited.Where(element => element.Name.LocalName == "param"))
                {
                    await Assert.That(parameterNames).Contains(param.Attribute("name")!.Value);
                }

                inheritingMethods.Add(extensionMethod.Name);
            }

            // The public method in a private nested type resolves too: the metadata import keeps nested types of
            // any accessibility and leaves out only non-public methods. Visual Studio's hover from a package agrees.
            await Assert.That(inheritingMethods.OrderBy(name => name, StringComparer.Ordinal)).IsEquivalentTo(
                new[] { "IsDivisibleByThree", "IsMultipleOf", "IsMultipleOf", "IsSameValueAs", "IsTrimmed", "IsUpperCase" },
                TUnit.Assertions.Enums.CollectionOrdering.Matching);

            // Why non-public source methods keep the placeholder: the XML file documents the internal method,
            // but a package consumer cannot see it, so a cref to it would leave the hover empty.
            var internalMethodId = library.GetTypeByMetadataName("TUnit.Assertions.Tests.TestData.DocumentedMethodAssertions")!
                .GetMembers("IsPowerOfTwo").Single().GetDocumentationCommentId()!;
            await Assert.That(members.Select(member => member.Attribute("name")!.Value)).Contains(internalMethodId);
            await Assert.That(DocumentationCommentId.GetFirstSymbolForDeclarationId(internalMethodId, consumer)).IsNull();
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
