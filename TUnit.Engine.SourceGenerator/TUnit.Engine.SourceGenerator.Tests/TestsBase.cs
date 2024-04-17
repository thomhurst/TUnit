using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

public class TestsBase
{
    protected TestsBase()
    {
    }
    
    public async Task RunTest(string inputFile, Action<string[]> assertions)
    {
        var source = await File.ReadAllTextAsync(inputFile);
        
        // Create an instance of the source generator.
        var generator = new TestsGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // To run generators, we can use an empty compilation.

        var compilation = CSharpCompilation.Create(
                GetType().Name,
                new[] { CSharpSyntaxTree.ParseText(source) },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddReferences(ReferencesHelper.References);
        
        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .Except([source])
            .Reverse()
            .ToArray();

        assertions(generatedFiles);
    }
}