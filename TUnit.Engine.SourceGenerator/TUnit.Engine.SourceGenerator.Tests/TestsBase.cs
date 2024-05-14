using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TestsBase<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    protected TestsBase()
    {
    }

    public Task RunTest(string inputFile, Action<string[]> assertions)
    {
        return RunTest(inputFile, new RunTestOptions(), assertions);
    }
    
    public async Task RunTest(string inputFile, RunTestOptions runTestOptions, Action<string[]> assertions)
    {
        var source = await File.ReadAllTextAsync(inputFile);

        var additionalSources =
            await Task.WhenAll(runTestOptions.AdditionalFiles.Select(x => File.ReadAllTextAsync(x)));
        
        // Create an instance of the source generator.
        var generator = new TGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);
        
        // To run generators, we can use an empty compilation.

        var compilation = CSharpCompilation.Create(
                GetType().Name,
                new[] { CSharpSyntaxTree.ParseText(source) },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddReferences(ReferencesHelper.References)
            .AddSyntaxTrees(additionalSources.Select(x => CSharpSyntaxTree.ParseText(x)));
        
        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .Except([source])
            .Except(additionalSources)
            .ToArray();

        assertions(generatedFiles);
    }
}