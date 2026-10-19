using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// [AssertionFrom] with an unbound generic type, such as typeof(Memory&lt;&gt;), used to generate nothing and report
/// nothing (#7132). It now generates assertions that are generic over the type's parameters, and reports TU0001
/// when it cannot.
/// </summary>
internal class UnboundGenericAssertionFromTests : TestsBase
{
    private static readonly string TestDataPath = Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "TestData",
        "UnboundGenericAssertionFrom.cs");

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    private const string CallSites = """
        using System.Threading.Tasks;
        using TUnit.Assertions;
        using TUnit.Assertions.Extensions;
        using TUnit.Assertions.Tests.TestData;

        public static class UnboundGenericCallSites
        {
            public static async Task Use()
            {
                var box = new Box<int>(1, 2);
                await Assert.That(new Box<int>()).IsEmptyBox();
                await Assert.That(box).IsNotEmptyBox();
                await Assert.That(box).HoldsItem(2);

                var pair = new Pair<string, int> { Key = "key" };
                await Assert.That(pair).HasKey();

                var shadow = new Shadow<object> { Value = "text" };
                await Assert.That(shadow).HoldsValueOfType<object, string>();
                await Assert.That(shadow).IsDefaultOf<object, int>(0);

                await Assert.That(new Outer<int>.Inner<string> { IsSet = true }).IsSet();

                await Assert.That(new Box<int>()).IsEmptyIntBox();
            }
        }
        """;

    [Test]
    public Task GeneratesGenericAssertionsForUnboundGenericTypes() => RunTest(
        TestDataPath,
        async generatedFiles =>
        {
            var file = generatedFiles.Single(f => f.Contains("partial class UnboundGenericAssertions"));

            await Assert.That(file).Contains("public class BoxIsEmptyAssertion<T> : Assertion<TUnit.Assertions.Tests.TestData.Box<T>>");
            await Assert.That(file).Contains("IsEmptyBox<T>(this IAssertionSource<TUnit.Assertions.Tests.TestData.Box<T>> source)");
            await Assert.That(file).Contains("var result = actualValue.IsEmpty;");
            await Assert.That(file).Contains("HoldsItem<T>(this IAssertionSource<TUnit.Assertions.Tests.TestData.Box<T>> source, T item");
            await Assert.That(file).Contains("public class PairHasKeyAssertion<TKey, TValue> : Assertion<TUnit.Assertions.Tests.TestData.Pair<TKey, TValue>>");
            await Assert.That(file).Contains("where TKey : notnull");

            // A method type parameter that cannot be inferred is passed explicitly
            await Assert.That(file).Contains("var result = actualValue.HoldsValueOfType<TOther>();");

            // Reused type parameter names are made distinct, and every use refers to the right one
            await Assert.That(file).Contains("public class ShadowIsDefaultOfWithTAssertion<T, T1> : Assertion<TUnit.Assertions.Tests.TestData.Shadow<T>>");
            await Assert.That(file).Contains("IsDefaultOf<T, T1>(this IAssertionSource<TUnit.Assertions.Tests.TestData.Shadow<T>> source, T1 value");
            await Assert.That(file).Contains("var result = actualValue.IsDefaultOf<T1>(_value);");
            await Assert.That(file).Contains("public class InnerIsSetAssertion<T, T1> : Assertion<TUnit.Assertions.Tests.TestData.Outer<T>.Inner<T1>>");

            // The generic and non-generic classes for Box<>.IsEmpty and Box<int>.IsEmpty are both emitted
            await Assert.That(file).Contains("public class BoxIsEmptyAssertion : Assertion<TUnit.Assertions.Tests.TestData.Box<int>>");

            // The generated code, used from call sites on closed types, must compile
            var source = await ReadAllTextAsync(TestDataPath);
            var compilation = CSharpCompilation.Create(
                "UnboundGenericAssertionFromCompileCheck",
                [
                    CSharpSyntaxTree.ParseText(source, ParseOptions),
                    CSharpSyntaxTree.ParseText(CallSites, ParseOptions),
                    ..generatedFiles.Select(f => CSharpSyntaxTree.ParseText(f, ParseOptions))
                ],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
                // See CompileChecker: Polyfill's internal CallerArgumentExpressionAttribute on net472
                .Where(d => !string.Equals(d.Id, "CS0122", StringComparison.Ordinal))
#endif
                .Select(d => d.ToString())
                .ToArray();

            await Assert.That(errors).IsEmpty();
        });

    [Test]
    public async Task Reports_Missing_Member_On_Unbound_Generic_Type()
    {
        var diagnostics = RunGenerator("""
            using TUnit.Assertions.Attributes;

            namespace Repro;

            public class Box<T>
            {
                public bool IsEmpty => true;
            }

            [AssertionFrom(typeof(Box<>), "DoesNotExist")]
            public static partial class BoxAssertions;
            """);

        var diagnostic = diagnostics.Single(d => d.Id == "TU0001");
        await Assert.That(diagnostic.GetMessage()).Contains("'DoesNotExist'");
        await Assert.That(diagnostic.GetMessage()).Contains("Repro.Box<T>");
    }

    [Test]
    public async Task Reports_Unbound_Generic_Member_Declared_On_Another_Type()
    {
        var diagnostics = RunGenerator("""
            using TUnit.Assertions.Attributes;

            namespace Repro;

            public class Box<T>
            {
            }

            public static class BoxHelpers
            {
                public static bool IsFull<T>(Box<T> box) => true;
            }

            [AssertionFrom(typeof(Box<>), typeof(BoxHelpers), nameof(BoxHelpers.IsFull))]
            public static partial class BoxAssertions;
            """);

        var diagnostic = diagnostics.Single(d => d.Id == "TU0001");
        await Assert.That(diagnostic.GetMessage()).Contains("unbound generic type is only supported");
    }

    [Test]
    public async Task Same_Member_Name_In_Two_Namespaces_Gets_Distinct_Classes()
    {
        var (generatedFiles, errors) = RunGeneratorAndCompile("""
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Attributes;
            using TUnit.Assertions.Extensions;

            namespace Repro.One
            {
                public class Box<T> { public bool IsEmpty => true; }
                public class Bag { public bool IsEmpty => true; }

                [AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty))]
                [AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]
                [AssertionFrom(typeof(Bag), nameof(Bag.IsEmpty))]
                public static partial class OneAssertions;
            }

            namespace Repro.Two
            {
                public class Box<T> { public bool IsEmpty => true; }
                public class Bag { public bool IsEmpty => true; }

                [AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty))]
                [AssertionFrom(typeof(Box<>), nameof(Box<int>.IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]
                [AssertionFrom(typeof(Bag), nameof(Bag.IsEmpty))]
                public static partial class TwoAssertions;
            }

            public static class CallSites
            {
                public static async Task Use()
                {
                    await Assert.That(new Repro.One.Box<int>()).IsEmpty();
                    await Assert.That(new Repro.Two.Box<int>()).IsEmpty();
                    await Assert.That(new Repro.One.Box<int>()).IsNotEmpty();
                    await Assert.That(new Repro.Two.Box<int>()).IsNotEmpty();
                    await Assert.That(new Repro.One.Bag()).IsEmpty();
                    await Assert.That(new Repro.Two.Bag()).IsEmpty();
                }
            }
            """);

        await Assert.That(errors).IsEmpty();

        var generated = string.Join("\n", generatedFiles);
        await Assert.That(generated).Contains("public class BoxIsEmptyAssertion<T> : Assertion<Repro.One.Box<T>>");
        await Assert.That(generated).Contains("public class ReproTwoBoxIsEmptyAssertion<T> : Assertion<Repro.Two.Box<T>>");
        await Assert.That(generated).Contains("public class BagIsEmptyAssertion : Assertion<Repro.One.Bag>");
        await Assert.That(generated).Contains("public class ReproTwoBagIsEmptyAssertion : Assertion<Repro.Two.Bag>");
    }

    private static (string[] GeneratedFiles, string[] Errors) RunGeneratorAndCompile(string source)
    {
        var compilation = CSharpCompilation.Create(
            "UnboundGenericAssertionFromCollisions",
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AssertionMethodGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedFiles = driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()).ToArray();
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
            // See CompileChecker: Polyfill's internal CallerArgumentExpressionAttribute on net472
            .Where(d => !string.Equals(d.Id, "CS0122", StringComparison.Ordinal))
#endif
            .Select(d => d.ToString())
            .ToArray();

        return (generatedFiles, errors);
    }

    private static Diagnostic[] RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            "UnboundGenericAssertionFromDiagnostics",
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AssertionMethodGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Diagnostics.ToArray();
    }

    private static async Task<string> ReadAllTextAsync(string path)
    {
#if NET
        return await File.ReadAllTextAsync(path);
#else
        await Task.CompletedTask;
        return File.ReadAllText(path);
#endif
    }
}
