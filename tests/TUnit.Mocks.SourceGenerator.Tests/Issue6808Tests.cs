using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class Issue6808Tests : SnapshotTestBase
{
    private static void AssertEventCodeCompiles(string source, MetadataReference[]? additionalReferences = null)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var references = GetCachedReferences().Concat(additionalReferences ?? []);
        var input = CSharpCompilation.Create("RefStructEventRegression",
            [CSharpSyntaxTree.ParseText(source, options)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MockGenerator().AsSourceGenerator()], parseOptions: options);
        driver.RunGeneratorsAndUpdateCompilation(input, out var output, out var diagnostics);

        // The harness's Roslyn predates C# 14. Omit only the unchanged convenience files
        // using extension blocks; compile and emit the implementations, typed bridges,
        // raise extensions, setup wrappers, and their call sites without filtering errors.
        // RefStructEventTests compiles and executes the complete output with the SDK.
        output = output.RemoveSyntaxTrees(output.SyntaxTrees.Where(tree =>
            tree.FilePath.EndsWith("_MockEvents.g.cs", StringComparison.Ordinal)
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

    private const string CustomDelegateSource = """
        using System;
        using TUnit.Mocks;
        using TUnit.Mocks.Generated;

        public ref struct Payload { public int Value; }
        public delegate void PayloadHandler(Payload payload);
        public delegate void BufferHandler(string name, ReadOnlySpan<byte> bytes, Payload payload);

        public interface IEvents
        {
            event PayloadHandler? Changed;
            event BufferHandler? Buffered;
            event EventHandler<string>? Ordinary;
            void Execute();
            int Query();
        }

        public class Usage
        {
            public void Test()
            {
                var mock = Mock.Of<IEvents>(MockBehavior.Strict);
                mock.RaiseChanged(new Payload { Value = 42 });
                mock.RaiseBuffered("data", new byte[] { 1, 2 }, new Payload());
                mock.Execute().Callback(() => mock.RaiseChanged(new Payload()));
                mock.Query().Returns(1).RaisesOrdinary("done");
            }
        }
        """;

    [Test]
    public async Task RefStruct_Events_Compile_Without_Deferred_Boxing_Helpers()
    {
        AssertEventCodeCompiles(CustomDelegateSource);
        var generated = string.Join("\n", RunGenerator(CustomDelegateSource));
        await Assert.That(generated).DoesNotContain("RaisesChanged(");
        await Assert.That(generated).DoesNotContain("RaisesBuffered(");
        await Assert.That(generated).Contains("RaisesOrdinary(");
    }

    [Test]
    public Task RefStruct_Events_Generation_Snapshot()
        => VerifyGeneratorOutput(CustomDelegateSource);

#if NET10_0_OR_GREATER
    [Test]
    [Arguments("Strict")]
    [Arguments("Loose")]
    public void Reported_Nested_EventHandler_Reproduction_Compiles(string behavior)
    {
        AssertEventCodeCompiles($$"""
            using System;
            using TUnit.Mocks;

            public class RefStructEvent
            {
                public void SomeTest()
                {
                    var fooMock = Mock.Of<IFoo>(MockBehavior.{{behavior}});
                }

                public ref struct FooRefStruct { }
                public interface IFoo
                {
                    event EventHandler<FooRefStruct> FooRefStructEvent;
                }
            }
            """);
    }

    [Test]
    [Arguments("EventHandler<Payload>", "new Payload()")]
    [Arguments("EventHandler<ReadOnlySpan<byte>>", "new byte[] { 1 }")]
    [Arguments("EventHandler<Span<byte>>", "new byte[] { 1 }")]
    public void EventHandler_RefStruct_Arguments_Can_Be_Raised(string handlerType, string argument)
    {
        AssertEventCodeCompiles($$"""
            using System;
            using TUnit.Mocks;
            using TUnit.Mocks.Generated;
            public ref struct Payload { }
            public interface IEvents
            {
                event {{handlerType}}? Changed;
                void Execute();
                int Query();
            }
            public class Usage
            {
                public void Test()
                {
                    var mock = Mock.Of<IEvents>();
                    mock.RaiseChanged({{argument}});
                }
            }
            """);
    }
#endif

    [Test]
    [Arguments("Mock.Of<Service>()")]
    [Arguments("Mock.Wrap(new Service())")]
    public void Class_Events_Compile(string creation)
    {
        AssertEventCodeCompiles($$"""
            using System;
            using TUnit.Mocks;
            using TUnit.Mocks.Generated;
            public delegate void BufferHandler(ReadOnlySpan<byte> bytes);
            public class Service
            {
                public virtual event BufferHandler? Changed;
                public virtual void Execute() { }
            }
            public class Usage
            {
                public void Test()
                {
                    var mock = {{creation}};
                    mock.RaiseChanged(new byte[] { 1 });
                }
            }
            """);
    }

    [Test]
    public void Generic_And_Inherited_Events_Compile()
    {
        AssertEventCodeCompiles("""
            using System;
            using TUnit.Mocks;
            using TUnit.Mocks.Generated;
            [assembly: GenerateMock(typeof(IEvents<>))]
            public delegate void BufferHandler<T>(ReadOnlySpan<T> bytes) where T : unmanaged;
            public interface IBase<T> where T : unmanaged
            {
                event BufferHandler<T>? Changed;
            }
            public interface IEvents<T> : IBase<T> where T : unmanaged { }
            public class Usage
            {
                public void Test()
                {
                    var mock = Mock.Of<IEvents<int>>();
                    mock.RaiseChanged(new int[] { 1 });
                }
            }
            """);
    }

    [Test]
    public void Multi_Type_Primary_And_Secondary_Events_Compile_Across_Combinations()
    {
        AssertEventCodeCompiles("""
            using System;
            using TUnit.Mocks;
            using TUnit.Mocks.Generated;
            public delegate void BufferHandler(ReadOnlySpan<byte> bytes);
            public interface IPrimary { event BufferHandler? Primary; }
            public interface ISecondary { event BufferHandler? Secondary; }
            public interface IExtra { }
            public class Usage
            {
                public void Test()
                {
                    var pair = Mock.Of<IPrimary, ISecondary>();
                    pair.RaisePrimary(new byte[] { 1 });
                    pair.RaiseSecondary(new byte[] { 2 });
                    var triple = Mock.Of<IPrimary, IExtra, ISecondary>();
                    triple.RaisePrimary(new byte[] { 3 });
                    triple.RaiseSecondary(new byte[] { 4 });
                }
            }
            """);
    }

    [Test]
    public async Task Inaccessible_RefStruct_Event_Does_Not_Expose_A_Bridge()
    {
        var reference = CreateExternalAssemblyReference("""
            using System;
            public abstract class Service
            {
                protected delegate void BufferHandler(ReadOnlySpan<byte> bytes);
                protected abstract event BufferHandler Hidden;
            }
            """);
        const string source = """
            using TUnit.Mocks;
            public class Usage
            {
                public void Test() { var mock = Mock.Of<Service>(); }
            }
            """;
        AssertEventCodeCompiles(source, [reference]);
        var generated = string.Join("\n", RunGenerator(source, [reference]));
        await Assert.That(generated).DoesNotContain("Hidden_Raiser");
        await Assert.That(generated).DoesNotContain("RaiseHidden(");
    }
}
