using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;
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
        
        Assert.Empty(diagnostics);
        
        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
        
        Assert.Empty(generatorDiagnostics);
        
        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();
        
        // Verify key elements are generated
        Assert.Contains("TestMetadataRegistry", generatedSource);
        Assert.Contains("CreateTestMetadata_0", generatedSource);
        Assert.Contains("TestClass.SimpleTest", generatedSource);
        Assert.Contains("TestSourceRegistrar.RegisterMetadata", generatedSource);
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
        
        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        
        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();
        
        // Verify data sources are generated
        Assert.Contains("InlineDataSourceProvider(1, 2, 3)", generatedSource);
        Assert.Contains("InlineDataSourceProvider(4, 5, 6)", generatedSource);
        Assert.Contains("GetMethodDataSources_0", generatedSource);
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
        
        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        
        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();
        
        // Verify method data source is generated
        Assert.Contains("MethodDataSourceProvider", generatedSource);
        Assert.Contains("GetTestData", generatedSource);
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
        
        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        
        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();
        
        // Verify repeat and timeout are generated
        Assert.Contains("RepeatCount = 5", generatedSource);
        Assert.Contains("Timeout = TimeSpan.FromMilliseconds(1000)", generatedSource);
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
        
        var generator = new TestMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        
        var generatedSource = outputCompilation.SyntaxTrees.Last().ToString();
        
        // Verify skip is generated
        Assert.Contains("IsSkipped = true", generatedSource);
        Assert.Contains("SkipReason = \"Not implemented yet\"", generatedSource);
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