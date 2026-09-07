using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class Issue6734Tests : SnapshotTestBase
{
    private const string Source = """
        using TUnit.Mocks;

        public class ConstructorClient
        {
            public ConstructorClient() { Call(); }
            public ConstructorClient(string value) { Call(); }
            public ConstructorClient(int value) { Call(); }
            public virtual void Call() { }
        }

        public class WrappedConstructorClient
        {
            public WrappedConstructorClient(int value) { Call(); }
            public virtual void Call() { }
        }

        public class Usage
        {
            public void Test()
            {
                _ = Mock.Of<ConstructorClient>();
                _ = Mock.Wrap(new WrappedConstructorClient(42));
            }
        }
        """;

    [Test]
    public Task Class_Constructor_Callback_Initialization_Snapshot()
        => VerifyGeneratorOutput(Source);

    [Test]
    public void Constructor_Callback_Impl_And_Factory_Compile_With_CSharp11()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);
        // The convenience extension blocks require C# 14 already. Compile the changed
        // implementation/factory files separately to guard their existing C# 11 syntax.
        var implementations = RunGenerator(Source, parseOptions: options)
            .Where(source => source.Contains("MockConstructionContext", StringComparison.Ordinal))
            .ToArray();
        if (implementations.Length != 2)
        {
            throw new InvalidOperationException("Expected both partial and wrap mock implementations.");
        }
        var trees = implementations.Prepend(Source)
            .Select((source, index) => CSharpSyntaxTree.ParseText(source, options, $"Source{index}.cs"));
        var compilation = CSharpCompilation.Create("ConstructorCallbackCompatibility", trees,
            GetCachedReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        }
    }
}
