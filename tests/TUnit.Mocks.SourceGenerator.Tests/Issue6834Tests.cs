using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6834
// One type mocked both regularly and through Mock.Wrap produced two models differing only in
// IsWrapMock. Both emitted `{name}_MockImplFactory.g.cs` and `{name}_MockMembers.g.cs`, so the
// duplicate hint name aborted the generator and every mock in the compilation vanished with it.
public class Issue6834Tests : SnapshotTestBase
{
    private const string DualModeSource = """
        using TUnit.Mocks;

        namespace TestNamespace;

        public class Calculator
        {
            public virtual int Add(int a, int b) => a + b;
            public virtual int Value { get; set; }
        }

        public class Usage
        {
            void M()
            {
                _ = Mock.Of<Calculator>();
                _ = Mock.Wrap(new Calculator());
            }
        }
        """;

    [Test]
    public Task Type_Mocked_Regularly_And_Wrapped()
        => VerifyGeneratorOutput(DualModeSource);

    [Test]
    public async Task Each_Mock_Mode_Gets_Its_Own_Hint_Name()
    {
        var hintNames = RunGeneratorHintNames(DualModeSource);

        await Assert.That(hintNames).IsEquivalentTo(hintNames.Distinct());
        await Assert.That(hintNames).Contains("TestNamespace_Calculator_MockImplFactory.g.cs");
        await Assert.That(hintNames).Contains("TestNamespace_Calculator_WrapMockImplFactory.g.cs");
    }

    [Test]
    public async Task The_Member_Surface_Is_Emitted_Once()
    {
        var hintNames = RunGeneratorHintNames(DualModeSource);

        await Assert.That(hintNames.Count(name => name.EndsWith("_MockMembers.g.cs", StringComparison.Ordinal)))
            .IsEqualTo(1);
    }

    [Test]
    public async Task Wrapping_Without_A_Regular_Mock_Still_Emits_The_Member_Surface()
    {
        var hintNames = RunGeneratorHintNames("""
            using TUnit.Mocks;

            namespace TestNamespace;

            public class Calculator
            {
                public virtual int Add(int a, int b) => a + b;
            }

            public class Usage
            {
                void M() => _ = Mock.Wrap(new Calculator());
            }
            """);

        await Assert.That(hintNames).Contains("TestNamespace_Calculator_MockMembers.g.cs");
        await Assert.That(hintNames).Contains("TestNamespace_Calculator_WrapMockImplFactory.g.cs");
    }

    private static string[] RunGeneratorHintNames(string source)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("DualModeMock",
            [CSharpSyntaxTree.ParseText(source, options)], GetCachedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MockGenerator().AsSourceGenerator()], parseOptions: options);

        // A duplicate hint name surfaces here, either as a throw or as a generator diagnostic.
        var result = driver.RunGenerators(compilation).GetRunResult();
        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
        }

        return result.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).ToArray();
    }
}
