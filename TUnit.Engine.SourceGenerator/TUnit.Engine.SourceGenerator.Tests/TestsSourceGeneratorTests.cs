using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace TUnit.Engine.SourceGenerator.Tests;

public class TestsSourceGeneratorTests
{
    private const string DddRegistryText = @"User
Document
Customer";

    [Fact]
    public void GenerateClassesBasedOnDDDRegistry()
    {
        // Create an instance of the source generator.
        var generator = new TestsSourceGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // To run generators, we can use an empty compilation.
        var compilation = CSharpCompilation.Create(nameof(TestsSourceGeneratorTests));

        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => Path.GetFileName(t.FilePath))
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