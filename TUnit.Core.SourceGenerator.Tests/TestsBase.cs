using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal partial class TestsBase<TGenerator> where TGenerator : IIncrementalGenerator, new()
{
    protected TestsBase()
    {
    }

    public Task RunTest(string inputFile, Func<string[], Task> assertions)
    {
        return RunTest(inputFile, new RunTestOptions(), assertions);
    }

    public async Task RunTest(string inputFile, RunTestOptions runTestOptions, Func<string[], Task> assertions)
    {
#if NET
        var source = await File.ReadAllTextAsync(inputFile);
#else
        var source = File.ReadAllText(inputFile);
#endif

        string[] additionalSources =
        [
            """
            // <auto-generated/>
            global using global::System;
            global using global::System.Collections.Generic;
            global using global::System.IO;
            global using global::System.Linq;
            global using global::System.Net.Http;
            global using global::System.Threading;
            global using global::System.Threading.Tasks;
            global using global::TUnit.Core;
            global using static global::TUnit.Core.HookType;
            """,
#if NET
            ..await Task.WhenAll(runTestOptions.AdditionalFiles.Select(x => File.ReadAllTextAsync(x)))
#else
            ..runTestOptions.AdditionalFiles.Select(x => File.ReadAllText(x))
#endif
        ];

        // Create an instance of the source generator.
        var generator = new TGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        if (runTestOptions.BuildProperties != null)
        {
            driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(
                    runTestOptions.BuildProperties.ToImmutableDictionary()
                )
            );
        }

        // To run generators, we can use an empty compilation.

        var compilation = CSharpCompilation.Create(
                GetType().Name,
                [
                    CSharpSyntaxTree.ParseText(source)
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            )
            .WithReferences(ReferencesHelper.References)
            .AddSyntaxTrees(additionalSources.Select(x => CSharpSyntaxTree.ParseText(x)));

        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        foreach (var error in diagnostics.Where(IsError))
        {
            throw new Exception
            (
                $"""
                  There was an error with the compilation. 
                  Have you added required references and additional files?
                  
                  {error}
                  
                  {string.Join(Environment.NewLine, newCompilation.SyntaxTrees.Select(x => x.GetText()))}
                 """
            );
        }

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .Except([source])
            .Except(additionalSources)
            .ToArray();

        foreach (var error in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
        {
            throw new Exception(
                $"There was an error with the generator compilation.{Environment.NewLine}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, generatedFiles)}");
        }

        await assertions(generatedFiles);

        var verifyTask = Verify(generatedFiles);

        if (runTestOptions.VerifyConfigurator != null)
        {
            verifyTask = runTestOptions.VerifyConfigurator(verifyTask);
        }

        await verifyTask;
    }

    private static bool IsError(Diagnostic x)
    {
        if (x.Severity == DiagnosticSeverity.Error)
        {
            return true;
        }

        if (x.Severity == DiagnosticSeverity.Warning && x.GetMessage().Contains("failed to generate source"))
        {
            return true;
        }

        return false;
    }
}