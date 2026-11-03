using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.Tests.Shared;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyNamesWithDashesTests
{
    [Test]
    public async Task AssemblyNameWithDashes_ShouldGenerateValidVariableNames()
    {
        const string source = """
            using TUnit.Core;

            public class AssemblyHooks
            {
                [Before(HookType.Assembly)]
                public static void BeforeAssembly() { }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "test-assembly",
            [GeneratorTestInput.Parse(source)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new HookMetadataGenerator().AsSourceGenerator()],
            parseOptions: GeneratorTestInput.ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(driver.GetRunResult().GeneratedTrees).IsNotEmpty();
        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
    }
}
