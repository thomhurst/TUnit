using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using Xunit;

namespace TUnit.Engine.SourceGenerator.Tests;

public class RetryTestsTestsSourceGeneratorTests
{
    [Fact]
    public async Task GenerateClasses()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(Git.RootDirectory.FullName,
                "TUnit.TestProject",
                "RetryTests.cs")
        );
        // Create an instance of the source generator.
        var generator = new TestsSourceGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // To run generators, we can use an empty compilation.

        var compilation = CSharpCompilation.Create(
                nameof(RetryTestsTestsSourceGeneratorTests),
                new[] { CSharpSyntaxTree.ParseText(source) },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddReferences(ReferencesHelper.References);
        
        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .ToArray();

        // In this case, it is enough to check the file name.
        Assert.Equivalent(new[]
        {
            "User.g.cs",
            "Document.g.cs",
            "Customer.g.cs"
        }, generatedFiles);
    }
}