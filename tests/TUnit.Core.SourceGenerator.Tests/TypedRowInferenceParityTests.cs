using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TypedRowInferenceParityTests
{
    public static IEnumerable<object?[]> Shapes()
    {
        yield return ["T value", "int", "", "int"];
        yield return ["T[] value", "int[]", "", "int"];
        yield return ["T[,] value", "int[,]", "", "int"];
        yield return ["T[][] value", "int[][]", "", "int"];
        yield return ["T[] value", "int[,]", "", null];
        yield return ["T? value", "int", "where T : struct", "int"];
        yield return ["T? value", "int?", "where T : struct", "int"];
        yield return ["T value, params string[] rest", "int", "", "int"];
        yield return ["params T[] values", "int[]", "", "int"];
#if NET9_0_OR_GREATER
        yield return ["T value, params ReadOnlySpan<string> rest", "int", "", "int"];
        yield return ["T value, params IEnumerable<string> rest", "int", "", "int"];
        yield return ["params ReadOnlySpan<T> values", "ReadOnlySpan<int>", "", "int"];
        yield return ["params IEnumerable<T> values", "IEnumerable<int>", "", "int"];
#endif
        yield return ["T first, T second", "TestDataRow<Func<(int, int)>>", "", "int"];
        yield return ["T first, T second", "TestDataRow<Func<(int, string)>>", "", null];
        yield return ["T first, T second", "(int, int)", "", "int"];
        yield return ["T value", "object[]", "", null];
    }

    [Test]
    [MethodDataSource(nameof(Shapes))]
    public async Task Generator_and_reflection_infer_the_same_types(
        string parameters, string row, string constraints, string? expected)
    {
        // Compile the same declaration once: Roslyn symbols and emitted reflection metadata therefore
        // describe exactly the same method, including IsParams / ParamCollectionAttribute differences.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class Subject
            {
                public static void Run<T>({{parameters}}) {{constraints}} { }
                public static {{row}} Row() => throw new Exception();
            }
            """;
        var compilation = CSharpCompilation.Create(
            "TypedRowParity_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var assemblyBytes = new MemoryStream();
        var emitted = compilation.Emit(assemblyBytes);
        await Assert.That(emitted.Success).IsTrue()
            .Because(string.Join(Environment.NewLine, emitted.Diagnostics));

        var subject = compilation.GetTypeByMetadataName("Subject")!;
        var method = subject.GetMembers("Run").OfType<IMethodSymbol>().Single();
        var rowType = subject.GetMembers("Row").OfType<IMethodSymbol>().Single().ReturnType;
        var generatorInference = typeof(TestMetadataGenerator).Assembly
            .GetType("TUnit.Analyzers.Extensions.TypedDataSourceInference", throwOnError: true)!
            .GetMethod("InferMethodTypeArgumentsFromRowType", BindingFlags.Public | BindingFlags.Static)!;
        var generated = (ITypeSymbol[]?)generatorInference.Invoke(null, [method, rowType]);

        var runtimeSubject = System.Reflection.Assembly.Load(assemblyBytes.ToArray()).GetType("Subject", throwOnError: true)!;
        var reflectionInference = typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionTestDataCollector", throwOnError: true)!
            .GetMethod("InferTypesFromTypedRow", BindingFlags.NonPublic | BindingFlags.Static)!;
        var reflected = (Type[]?)reflectionInference.Invoke(null,
            [runtimeSubject.GetMethod("Run")!, runtimeSubject.GetMethod("Row")!.ReturnType, 1]);

        // Check the expected result as well as parity, so both modes returning null cannot hide a bug.
        await Assert.That(generated?.Single().ToDisplayString()).IsEqualTo(expected);
        await Assert.That(reflected?.Single()).IsEqualTo(expected is null ? null : typeof(int));
        await Assert.That(generated is null).IsEqualTo(reflected is null);
    }
}
