using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/6854.
/// In IDE workspaces (C# DevKit, Visual Studio, Rider) project references are
/// <see cref="CompilationReference"/>s rather than PE references, so attributes applied
/// in a referenced project keep an <see cref="AttributeData.ApplicationSyntaxReference"/>
/// into that project's syntax trees. Asking the consuming compilation for a semantic model
/// on such a tree throws "SyntaxTree is not part of the compilation" and crashes the generator.
/// </summary>
public class CrossCompilationAttributeTests
{
    private const string BaseSource =
        """
        using TUnit.Core;

        [Category("FromBase")]
        [Retry(3)]
        public abstract class BaseTests
        {
            [Test]
            [Arguments(1, 2)]
            [Timeout(5_000)]
            [DisplayName("Inherited $a $b")]
            public void Inherited(int a, int b)
            {
            }

            [Test]
            [Arguments(123_999.00000000000000001, Skip = "from base", Categories = new[] { "slow" })]
            [Arguments(-1.5)]
            [Arguments(new object[] { 2.5 })]
            public void InheritedDecimal(decimal value)
            {
            }
        }
        """;

    private const string DerivedSource =
        """
        using TUnit.Core;

        [InheritsTests]
        public class DerivedTests : BaseTests
        {
            [Test]
            public void Own()
            {
            }
        }
        """;

    [Test]
    public async Task AttributeWriter_FallsBackToTypedConstants_ForAttributesFromReferencedCompilation()
    {
        var baseCompilation = CreateCompilation("CrossCompilationBase", BaseSource);
        var derivedCompilation = CreateCompilation("CrossCompilationDerived", DerivedSource, baseCompilation.ToMetadataReference());

        var derivedType = derivedCompilation.GetTypeByMetadataName("DerivedTests")!;
        var baseAttributes = derivedType.BaseType!.GetAttributes();

        // Precondition: this is the IDE shape — syntax references exist but point into another compilation.
        await Assert.That(baseAttributes.Length).IsEqualTo(2);
        foreach (var attribute in baseAttributes)
        {
            await Assert.That(attribute.ApplicationSyntaxReference).IsNotNull();
            await Assert.That(derivedCompilation.ContainsSyntaxTree(attribute.ApplicationSyntaxReference!.SyntaxTree)).IsFalse();
        }

        var attributeWriter = new AttributeWriter(derivedCompilation);
        var codeWriter = new CodeWriter("", includeHeader: false);

        attributeWriter.WriteAttributes(codeWriter, baseAttributes);
        var output = codeWriter.ToString();

        await Assert.That(output).Contains("new global::TUnit.Core.CategoryAttribute(\"FromBase\")");
        await Assert.That(output).Contains("new global::TUnit.Core.RetryAttribute(3)");
    }

    [Test]
    public async Task TestMetadataGenerator_DoesNotThrow_ForAttributesFromReferencedCompilation()
    {
        var baseCompilation = CreateCompilation("CrossCompilationBase", BaseSource);
        var derivedCompilation = CreateCompilation(
            "CrossCompilationDerived",
            DerivedSource,
            baseCompilation.ToMetadataReference())
            .AddSyntaxTrees(
                Parse(
                    """
                    namespace System.Diagnostics.CodeAnalysis;

                    public class ExcludeFromCodeCoverageAttribute : Attribute;
                    """),
                Parse(
                    """
                    namespace System.Diagnostics.CodeAnalysis;

                    public class UnconditionalSuppressMessageAttribute : Attribute;
                    """));

        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TestMetadataGenerator().AsSourceGenerator()], parseOptions: ParseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(derivedCompilation, out var outputCompilation, out var generatorDiagnostics);

        var generatorResult = driver.GetRunResult().Results.Single();

        await Assert.That(generatorResult.Exception).IsNull();
        await Assert.That(generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();

        var generated = string.Join(Environment.NewLine, generatorResult.GeneratedSources.Select(s => s.SourceText.ToString()));

        // Class-level attributes inherited from the referenced project (per-class helper path)
        await Assert.That(generated).Contains("new global::TUnit.Core.CategoryAttribute(\"FromBase\")");
        await Assert.That(generated).Contains("new global::TUnit.Core.RetryAttribute(3)");

        // Method-level attributes on the inherited test method ([InheritsTests] path)
        await Assert.That(generated).Contains("new global::TUnit.Core.TimeoutAttribute(5000)");
        await Assert.That(generated).Contains("new global::TUnit.Core.DisplayNameAttribute(\"Inherited $a $b\")");
        await Assert.That(generated).Contains("new global::TUnit.Core.ArgumentsAttribute(1, 2)");

        // [Arguments] from the referenced project: numeric literals for decimal parameters keep their
        // source text (a double TypedConstant would round 123_999.00000000000000001), named arguments
        // are preserved, and a non-literal argument such as the array form uses the typed constant.
        await Assert.That(generated).Contains("new global::TUnit.Core.ArgumentsAttribute(123_999.00000000000000001m)");
        await Assert.That(generated).Contains("Skip = \"from base\"");
        await Assert.That(generated).Contains("Categories = ");
        await Assert.That(generated).Contains("\"slow\"");
        await Assert.That(generated).Contains("new global::TUnit.Core.ArgumentsAttribute(-1.5m)");
        await Assert.That(generated).Contains("new global::TUnit.Core.ArgumentsAttribute(2.5m)");

        var compilationErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();

        await Assert.That(compilationErrors).IsEmpty();
    }

    private static CSharpCompilation CreateCompilation(string assemblyName, string source, params MetadataReference[] additionalReferences)
    {
        return CSharpCompilation.Create(
            assemblyName,
            [Parse(source)],
            [..ReferencesHelper.References, ..additionalReferences],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    private static SyntaxTree Parse(string source)
    {
        return CSharpSyntaxTree.ParseText(source, ParseOptions);
    }
}
