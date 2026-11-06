using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertionExtensionXmlDocsTests : TestsBase<AssertionExtensionGenerator>
{
    private const string ClassPath = "/*[self::summary or self::remarks or self::typeparam]";

    private static readonly string FixturePath = Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "TestData",
        "DocumentedAssertionExtension.cs");

    private static readonly string[] InheritingMethods =
    [
        "HasDocumentedTag", "HasDocumentedTagOfType", "HasDocumentedTagOfType", "IsClassOnlyDocumented",
        "IsDocumentedTrue", "IsInDocumentedRange", "IsInDocumentedRange",
    ];

    [Test]
    public Task DocumentedAssertionExtension_InheritdocCrefsResolveToSource() => RunTest(FixturePath, async generatedFiles =>
    {
        // Compile the fixture with its generated code as a GenerateDocumentationFile build does.
        var trees = generatedFiles
            .Prepend(File.ReadAllText(FixturePath))
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
                var inheritdocs = XElement.Parse(extensionMethod.GetDocumentationCommentXml()!).Elements("inheritdoc").ToArray();
                if (inheritdocs.Length == 0)
                {
                    continue;
                }

                // The class comes first: Rider expands only the first inheritdoc.
                var assertionClass = extensionMethod.ReturnType.OriginalDefinition;
                var classCref = inheritdocs[0].Attribute("cref")!.Value;
                await Assert.That(SymbolEqualityComparer.Default.Equals(
                    DocumentationCommentId.GetFirstSymbolForDeclarationId(classCref, compilation), assertionClass)).IsTrue();
                await Assert.That(assertionClass.GetDocumentationCommentId()).IsEqualTo(classCref);
                await Assert.That(inheritdocs[0].Attribute("path")!.Value).IsEqualTo(ClassPath);

                // Then the constructor the extension wraps: its parameters after the context are the
                // extension's parameters after the receiver.
                if (inheritdocs.Length > 1)
                {
                    var constructorCref = inheritdocs[1].Attribute("cref")!.Value;
                    var constructor = (IMethodSymbol)DocumentationCommentId.GetFirstSymbolForDeclarationId(constructorCref, compilation)!;
                    var valueParameters = constructor.Parameters.Skip(1).Select(parameter => parameter.Name).ToArray();

                    await Assert.That(SymbolEqualityComparer.Default.Equals(constructor.ContainingType, assertionClass)).IsTrue();
                    await Assert.That(constructor.GetDocumentationCommentId()).IsEqualTo(constructorCref);
                    await Assert.That(extensionMethod.Parameters.Skip(1).Take(valueParameters.Length).Select(parameter => parameter.Name))
                        .IsEquivalentTo(valueParameters, TUnit.Assertions.Enums.CollectionOrdering.Matching);
                    await Assert.That(inheritdocs[1].Attribute("path")!.Value).IsEqualTo(
                        $"/*[(self::param and not(@name='{constructor.Parameters[0].Name}')) or self::exception]");
                }

                inheritingMethods.Add(extensionMethod.Name);
            }
        }

        await Assert.That(inheritingMethods.OrderBy(name => name, StringComparer.Ordinal))
            .IsEquivalentTo(InheritingMethods, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    });

    [Test]
    public Task DocumentedAssertionExtension_InheritdocCrefsResolveFromPackage() => RunTest(FixturePath, async generatedFiles =>
    {
        // Build the fixture and its generated code as a library with its XML documentation file, then
        // reference both from a second compilation, which sees the library as a package consumer does.
        var trees = generatedFiles
            .Prepend(File.ReadAllText(FixturePath))
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

        var inheritedDocs = new List<string>();
        foreach (var member in members)
        {
            var inheritdocs = member.Elements("inheritdoc").ToArray();
            if (inheritdocs.Length == 0)
            {
                continue;
            }

            // As an IDE does: find each cref among what the package exposes, read that member's docs from
            // the package's XML file, and apply the path.
            var extensionMethod = (IMethodSymbol?)DocumentationCommentId.GetFirstSymbolForDeclarationId(
                member.Attribute("name")!.Value, consumer);
            await Assert.That(extensionMethod).IsNotNull();

            var inherited = new List<XElement>();
            foreach (var inheritdoc in inheritdocs)
            {
                var source = DocumentationCommentId.GetFirstSymbolForDeclarationId(inheritdoc.Attribute("cref")!.Value, consumer);
                await Assert.That(source).IsNotNull();
                await Assert.That(source!.ContainingAssembly.Name).IsEqualTo("DocumentedLibrary");

                inherited.AddRange(XDocument.Parse(source.GetDocumentationCommentXml()!)
                    .XPathSelectElements("/*" + inheritdoc.Attribute("path")!.Value));
            }

            // Every inherited <param> and <typeparam> names one of the extension's own.
            var parameterNames = extensionMethod!.Parameters.Select(parameter => parameter.Name).ToArray();
            var typeParameterNames = extensionMethod.TypeParameters.Select(typeParameter => typeParameter.Name).ToArray();
            foreach (var element in inherited)
            {
                if (element.Name.LocalName == "param")
                {
                    await Assert.That(parameterNames).Contains(element.Attribute("name")!.Value);
                }
                else if (element.Name.LocalName == "typeparam")
                {
                    await Assert.That(typeParameterNames).Contains(element.Attribute("name")!.Value);
                }
            }

            inheritedDocs.Add($"{extensionMethod.Name}: " + string.Join(", ", inherited.Select(element =>
                element.Attribute("name") is { } name ? $"{element.Name.LocalName} {name.Value}" : element.Name.LocalName)));
        }

        // What each extension shows: the class's elements, then the wrapped constructor's.
        await Assert.That(inheritedDocs.OrderBy(entry => entry, StringComparer.Ordinal)).IsEquivalentTo(new[]
        {
            "HasDocumentedTag: summary, typeparam TTag, param tag",
            "HasDocumentedTagOfType: summary, typeparam TTag",
            "HasDocumentedTagOfType: summary, typeparam TTag",
            "IsClassOnlyDocumented: summary",
            "IsDocumentedTrue: summary",
            "IsInDocumentedRange: summary, remarks, param maximum",
            "IsInDocumentedRange: summary, remarks, param minimum, param maximum, exception",
        }, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Why only the wrapped constructors' accessibility matters: a package exposes a documented internal
        // type, but not a documented internal constructor, which therefore gets no extension.
        var internalType = library.GetTypeByMetadataName("TUnit.Assertions.Tests.TestData.DocumentedInternalType")!;
        await Assert.That(DocumentationCommentId.GetFirstSymbolForDeclarationId(internalType.GetDocumentationCommentId()!, consumer)).IsNotNull();

        var internalConstructorId = library.GetTypeByMetadataName("TUnit.Assertions.Tests.TestData.DocumentedRangeAssertion")!
            .InstanceConstructors.Single(constructor => constructor.DeclaredAccessibility == Accessibility.Internal)
            .GetDocumentationCommentId()!;
        await Assert.That(members.Select(member => member.Attribute("name")!.Value)).Contains(internalConstructorId);
        await Assert.That(DocumentationCommentId.GetFirstSymbolForDeclarationId(internalConstructorId, consumer)).IsNull();
    });
}
