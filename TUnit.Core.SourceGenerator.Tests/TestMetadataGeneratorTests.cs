using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator;
using Xunit;

namespace TUnit.Core.SourceGenerator.Tests;

public class TestMetadataGeneratorTests
{
    [Fact]
    public async Task GeneratesTestMetadata_ForSimpleTest()
    {
        var source = @"
using TUnit.Core;

public class TestClass
{
    [Test]
    public void SimpleTest()
    {
    }
}";

        var (compilation, diagnostics) = await CreateCompilationAsync(source);

        await Assert.That(diagnostics).IsEmpty();

        var generator = new UnifiedTestMetadataGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        await Assert.That(generatorDiagnostics).IsEmpty();

        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();

        // Verify key elements are generated
        await Assert.That(generatedSource.Contains("UnifiedTestMetadataRegistry")).IsTrue();
        await Assert.That(generatedSource.Contains("new TestMetadata")).IsTrue();
        await Assert.That(generatedSource.Contains("TestClass.SimpleTest")).IsTrue();
        await Assert.That(generatedSource.Contains("TestMetadataRegistry.RegisterSource")).IsTrue();
    }

    [Fact]
    public async Task GeneratesTestMetadata_WithArguments()
    {
        var source = @"
using TUnit.Core;

public class TestClass
{
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 6)]
    public void ParameterizedTest(int a, int b, int c)
    {
    }
}";

        var (compilation, _) = await CreateCompilationAsync(source);

        var generator = new UnifiedTestMetadataGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();

        // Verify data sources are generated
        await Assert.That(generatedSource.Contains("StaticTestDataSource")).IsTrue();
        await Assert.That(generatedSource.Contains("new object[] { 1, 2, 3 }")).IsTrue();
        await Assert.That(generatedSource.Contains("new object[] { 4, 5, 6 }")).IsTrue();
    }

    [Fact]
    public async Task GeneratesTestMetadata_WithMethodDataSource()
    {
        var source = @"
using TUnit.Core;
using System.Collections.Generic;

public class TestClass
{
    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public void DataDrivenTest(int value)
    {
    }
    
    public static IEnumerable<int> GetTestData()
    {
        yield return 1;
        yield return 2;
    }
}";

        var (compilation, _) = await CreateCompilationAsync(source);

        var generator = new UnifiedTestMetadataGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();

        // Verify method data source is generated
        await Assert.That(generatedSource.Contains("DynamicTestDataSource")).IsTrue();
        await Assert.That(generatedSource.Contains("GetTestData")).IsTrue();
    }

    [Fact]
    public async Task GeneratesTestMetadata_WithRepeatAndTimeout()
    {
        var source = @"
using TUnit.Core;

public class TestClass
{
    [Test]
    [Repeat(5)]
    [Timeout(1000)]
    public void RepeatedTest()
    {
    }
}";

        var (compilation, _) = await CreateCompilationAsync(source);

        var generator = new UnifiedTestMetadataGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();

        // Verify repeat and timeout are generated
        await Assert.That(generatedSource.Contains("RetryCount = 5")).IsTrue();
        await Assert.That(generatedSource.Contains("TimeoutMs = 1000")).IsTrue();
    }

    [Fact]
    public async Task GeneratesTestMetadata_WithSkip()
    {
        var source = @"
using TUnit.Core;

public class TestClass
{
    [Test]
    [Skip(""Not implemented yet"")]
    public void SkippedTest()
    {
    }
}";

        var (compilation, _) = await CreateCompilationAsync(source);

        var generator = new UnifiedTestMetadataGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();

        // Verify skip is generated
        await Assert.That(generatedSource.Contains("IsSkipped = true")).IsTrue();
        await Assert.That(generatedSource.Contains("SkipReason = \"Not implemented yet\"")).IsTrue();
    }

    private async Task<(Compilation, ImmutableArray<Diagnostic>)> CreateCompilationAsync(string source)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();

        return (compilation, diagnostics);
    }
}
