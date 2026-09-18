using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/discussions/6829
// The property model recorded only "has a setter", so an `init` accessor came out as a plain
// `set` — CS8854/CS8855, because an implementation must match the slot's accessor kind. The
// generated mock implementation, the typed wrapper's explicit forwards and the wrap/partial
// overrides all have to emit `init`, and because an init-only member is assignable only on
// `this`/`base` (CS8852), the forwarding paths dispatch through the engine instead.
public class Issue6829Tests : SnapshotTestBase
{
    private const string InitOnlyInterfaceSource = """
        using TUnit.Mocks;

        namespace TestNamespace;

        public interface IInitOnlyValue
        {
            int Value { get; init; }
            string Mutable { get; set; }
            string this[int index] { get; init; }
        }

        public class Usage
        {
            void M()
            {
                _ = IInitOnlyValue.Mock();
            }
        }
        """;

    private const string InitOnlyClassSource = """
        using TUnit.Mocks;

        namespace TestNamespace;

        public abstract class AbstractInitOnly
        {
            public abstract int Value { get; init; }
            public abstract string this[int index] { get; init; }
        }

        public class VirtualInitOnly
        {
            public virtual int Value { get; init; }
        }

        public interface IInitOnlyValue
        {
            int Value { get; init; }
        }

        public class Usage
        {
            public void Test()
            {
                _ = Mock.Of<IInitOnlyValue>();
                _ = Mock.Of<AbstractInitOnly>();
                _ = Mock.Of<VirtualInitOnly>();
                _ = Mock.Wrap(new VirtualInitOnly());
            }
        }
        """;

    // Same signature, different setter kinds: neither slot can be dropped and one member cannot
    // implement both, so the `set` slot is split off into an explicit interface implementation that
    // dispatches on the shared member ids.
    private const string MixedSetterKindsSource = """
        using TUnit.Mocks;

        namespace TestNamespace;

        public interface IInitSlot
        {
            int V { get; init; }
            string this[int i] { get; init; }
        }

        public interface ISetSlot
        {
            int V { get; set; }
            string this[int i] { get; set; }
        }

        public interface IBothSlots : IInitSlot, ISetSlot;

        public class Usage
        {
            void M()
            {
                _ = Mock.Of<IBothSlots>();
            }
        }
        """;

    [Test]
    public Task Interface_With_Init_Only_Members_Emits_Init_Accessors()
        => VerifyGeneratorOutput(InitOnlyInterfaceSource);

    [Test]
    public Task Clashing_Setter_Kinds_Split_Into_Explicit_Slots()
        => VerifyGeneratorOutput(MixedSetterKindsSource);

    [Test]
    public void Clashing_Setter_Kinds_Compile()
        => AssertMockImplementationsCompile(MixedSetterKindsSource);

    [Test]
    public void Init_Only_Members_Compile_For_Every_Mock_Shape()
        => AssertMockImplementationsCompile(InitOnlyClassSource);

    /// <summary>
    /// Compiles the generated implementations, wrapper and factories. The convenience files built
    /// from C# 14 extension blocks are omitted because the harness's Roslyn predates them — the
    /// runtime <c>Issue6829Tests</c> in TUnit.Mocks.Tests compiles the complete output with the SDK.
    /// </summary>
    private static void AssertMockImplementationsCompile(string source)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var input = CSharpCompilation.Create("InitOnlyRegression",
            [CSharpSyntaxTree.ParseText(source, options)], GetCachedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MockGenerator().AsSourceGenerator()], parseOptions: options);
        driver.RunGeneratorsAndUpdateCompilation(input, out var output, out var diagnostics);

        output = output.RemoveSyntaxTrees(output.SyntaxTrees.Where(tree =>
            tree.FilePath.EndsWith("_MockMembers.g.cs", StringComparison.Ordinal)
            || tree.FilePath.EndsWith("_MockSecondaryMembers.g.cs", StringComparison.Ordinal)
            || tree.FilePath.EndsWith("_MockStaticExtension.g.cs", StringComparison.Ordinal)));

        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        var errors = diagnostics.Concat(result.Diagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
        }
    }
}
