using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

// Regression tests for https://github.com/thomhurst/TUnit/issues/7133: assertion types declared in
// the global namespace made the generators emit `using <global namespace>;`, which breaks the build.
// Each test compiles the input source together with the generated output and requires zero errors.

internal class GlobalNamespaceAssertionFromTests : TestsBase<AssertionMethodGenerator>
{
    [Test]
    public Task AssertionFrom_InGlobalNamespace_Compiles() => RunTest(
        GlobalNamespaceCompileCheck.TestDataPath("GlobalNamespaceAssertionFrom.cs"),
        generatedFiles => GlobalNamespaceCompileCheck.AssertCompiles("GlobalNamespaceAssertionFrom.cs", generatedFiles));
}

internal class GlobalNamespaceGenerateAssertionTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task GenerateAssertion_InGlobalNamespace_Compiles() => RunTest(
        GlobalNamespaceCompileCheck.TestDataPath("GlobalNamespaceGenerateAssertion.cs"),
        generatedFiles => GlobalNamespaceCompileCheck.AssertCompiles("GlobalNamespaceGenerateAssertion.cs", generatedFiles));
}

internal class GlobalNamespaceAssertionExtensionTests : TestsBase<AssertionExtensionGenerator>
{
    [Test]
    public Task AssertionExtension_InGlobalNamespace_Compiles() => RunTest(
        GlobalNamespaceCompileCheck.TestDataPath("GlobalNamespaceAssertionExtension.cs"),
        generatedFiles => GlobalNamespaceCompileCheck.AssertCompiles("GlobalNamespaceAssertionExtension.cs", generatedFiles));
}

internal class GlobalNamespaceCollectionShapeAssertionTests : TestsBase<CollectionShapeAssertionGenerator>
{
    // The trigger attribute is internal to TUnit.Assertions, so declare the same stand-in that
    // CollectionShapeAssertionSource.cs uses; the generator matches it by metadata name.
    private const string AttributeStandIn = """
        namespace TUnit.Assertions.Attributes
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            internal sealed class GenerateCollectionShapeAssertionsAttribute : System.Attribute;
        }
        """;

    [Test]
    public Task CollectionShapeWrapper_InGlobalNamespace_EmitsValidTypeName() => RunTest(
        GlobalNamespaceCompileCheck.TestDataPath("GlobalNamespaceCollectionShapeAssertionSource.cs"),
        new RunTestOptions { AdditionalSyntaxes = [AttributeStandIn] },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var generated = generatedFiles[0];
            await Assert.That(generated).DoesNotContain("<global namespace>");
            await Assert.That(generated).Contains("global::GlobalNamespaceCollectionShapeAssertionSource<");

            // The emitted forwarders call internal TUnit.Assertions constructors, so the output only compiles
            // inside TUnit.Assertions itself. A syntax check is what catches `global::<global namespace>.X`.
            var syntaxErrors = GeneratorTestInput.Parse(generated)
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{d.Id}: {d.GetMessage()}")
                .ToArray();

            await Assert.That(syntaxErrors).IsEmpty();
        });
}

internal static class GlobalNamespaceCompileCheck
{
    public static string TestDataPath(string testDataFile) => Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "TestData",
        testDataFile);

    public static async Task AssertCompiles(string testDataFile, string[] generatedFiles)
    {
        await Assert.That(generatedFiles).IsNotEmpty();

        foreach (var generatedFile in generatedFiles)
        {
            await Assert.That(generatedFile).DoesNotContain("<global namespace>");
        }

        var source = await File.ReadAllTextAsync(TestDataPath(testDataFile));

        var trees = generatedFiles
            .Prepend(source)
            .Select(text => GeneratorTestInput.Parse(text))
            .ToArray();

        // TestData files are also compiled into this test assembly, so referencing it would declare
        // every global-namespace TestData type twice. Exclude it so only the parsed source defines them.
        var thisAssemblyLocation = typeof(GlobalNamespaceCompileCheck).Assembly.Location;
        var references = ReferencesHelper.References
            .Where(r => !string.Equals(r.FilePath, thisAssemblyLocation, StringComparison.OrdinalIgnoreCase));

        var compilation = CSharpCompilation.Create(
            "GlobalNamespaceCompileCheck",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // See XmlDocsRegressionTests.AssertNoCompilationErrors for why CS0122 is filtered on net472.
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
            .Where(d => !string.Equals(d.Id, "CS0122", StringComparison.Ordinal))
#endif
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();

        await Assert.That(errors).IsEmpty();
    }
}
