using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/7099.
///
/// A trailing <c>params T[]</c> (or plain <c>T[]</c>) on a generic test method closed through
/// <c>[GenerateGenericTest]</c> or inference used to emit <c>typeof(T[])</c> in its parameter
/// metadata and <c>CastHelper.Cast&lt;T[]&gt;(args[0])</c> in its invoker, which fail with CS0246
/// and CS1503. The snapshot harness only inspects generator diagnostics, so this test compiles
/// the generated output and asserts there are no compiler errors.
/// </summary>
internal class GenericParamsArrayCompilationTests
{
    private const string Source =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.Linq;
        global using global::System.Threading.Tasks;
        global using global::TUnit.Core;

        namespace MyTests;

        public class ParamsGenericTests
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1, 2, 3)]
            public void Explicit_Many<T>(params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(string))]
            [Arguments("a")]
            public void Explicit_One_Reference<T>(params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments]
            public void Explicit_Zero<T>(params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments("label", 1, 2)]
            public void Explicit_Leading<T>(string label, params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(new[] { 4, 5 })]
            public void Explicit_ArrayArgument<T>(params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1, 2)]
            public void Explicit_PlainArray<T>(T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(new[] { 1, 2 }, 2)]
            public void Explicit_NonTrailingArray<T>(T[] values, int count) { }

            [Test]
            [Arguments(1, 2, 3)]
            public void Inferred_FromLeading<T>(T first, params T[] rest) { }

            [Test]
            [Arguments(1, 2, 3)]
            public void Inferred_FromParams<T>(params T[] values) { }

            [Test]
            [Arguments(new[] { 4, 5 })]
            public void Inferred_FromArrayArgument<T>(params T[] values) { }
        }

        public class ReviewShapes
        {
            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1, 2)]
            public void Optional_Before_Params<T>(int count = 0, params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments]
            public void Optional_Omitted_Before_Params<T>(int count = 42, params T[] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [MethodDataSource(nameof(Grids))]
            public void MultiDim<T>(int[,] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [MethodDataSource(nameof(Grids))]
            public void MultiDimGeneric<T>(T[,] values) { }

            public static IEnumerable<int[,]> Grids() => [new int[,] { { 1 } }];

            [Test]
            [Arguments(1, 2)]
            public void NonGeneric_Optional_Before_Params(int count = 0, params int[] values) { }

            [Test]
            [MethodDataSource(nameof(Grids))]
            public void NonGeneric_MultiDim(int[,] values) { }

            [Test]
            [GenerateGenericTest(typeof(int))]
            [Arguments(1, 2)]
            public void Optional_Before_Array_With_Token<T>(int count = 42, T[] values = null, global::System.Threading.CancellationToken token = default) { }

            [Test]
            [Arguments]
            public void NonGeneric_Optional_Omitted_Before_Array_With_Token(int count = 42, int[] values = null, global::System.Threading.CancellationToken token = default) { }

            [Test]
            [Arguments(new[] { 1 }, new[] { 2 })]
            public void Inferred_ArrayElements<T>(T first, params T[] rest) { }

            [Test]
            [Arguments(1, 2, 3)]
            public void Inferred_FromLeading_PlainArray<T>(T first, T[] rest) { }
        }

        public abstract class InheritedBase<T>
        {
            [Test]
            [GenerateGenericTest(typeof(string))]
            [Arguments(1, 2)]
            public void Inherited<U>(params T[] values) { }
        }

        [InheritsTests]
        public class InheritedDerived : InheritedBase<int>
        {
        }

        [GenerateGenericTest(typeof(int))]
        public class GenericClassParamsTests<T>
        {
            [Test]
            [Arguments(1, 2)]
            public void ClassTypeParameter(params T[] values) { }
        }
        """;

    [Test]
    public async Task Generated_generic_params_binding_compiles()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
                "GenericParamsArray",
                [CSharpSyntaxTree.ParseText(Source, parseOptions)],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        var generated = string.Join(
            Environment.NewLine + "----" + Environment.NewLine,
            output.SyntaxTrees.Skip(1).Select(t => t.GetText().ToString()));

        await Assert.That(errors)
            .IsEmpty()
            .Because($"generated code must compile. Errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, errors.Select(e => e.ToString()))
                + Environment.NewLine + generated);

        // A lone array value for params T[] infers T in both engine modes.
        await Assert.That(generated).Contains("Inferred_FromArrayArgument<int>");
    }
}
