using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ErrorSymbolHandlingTests
{
    [Test]
    public async Task TestMetadataGenerator_SkipsClassesWithErrorSymbols()
    {
        // This test verifies that TestMetadataGenerator correctly skips classes with TypeKind.Error
        // The test uses a compilation with missing references to simulate error symbols
        
        const string source = """
            using TUnit.Core;
            
            namespace TestNamespace;
            
            // This class will have error symbols due to missing references
            public class TestClass : NonExistentBaseClass  
            {
                [Test]
                public void TestMethod()
                {
                    // This test should not generate source code
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Create a compilation without references to cause ErrorSymbols
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references: [], // Deliberately empty to cause errors
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        // Run the generator
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);
        
        // Verify that no source was generated due to error symbols
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .Except([source])
            .ToArray();
            
        await Assert.That(generatedFiles).HasCount().EqualTo(0);
        
        // Verify no generator diagnostics were thrown (the generator should handle errors gracefully)
        var generatorErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith("TUnit"));
        await Assert.That(generatorErrors).HasCount().EqualTo(0);
    }
}