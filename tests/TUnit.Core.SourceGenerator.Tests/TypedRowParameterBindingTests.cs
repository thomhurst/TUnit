using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Analyzers;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TypedRowParameterBindingTests
{
    public static IEnumerable<object?[]> Cases()
    {
        yield return ["T[] seed, int count = 7", "int[]", "new int[] { 1 }", "seed, count", "int[]", "[1];7", true, "int"];
        yield return ["T[] seed, string[] rest", "int[]", "new int[] { 1 }", "seed, rest", "int[]", "[1];[]", false, "int"];
        yield return ["T[] seed, int count = 7, params string[] rest", "int[]", "new int[] { 1 }", "seed, count, rest", "int[]", "[1];7;[]", true, "int"];
        yield return ["T[] seed, params long[] rest", "(int[], int[])", "new int[] { 1 }, new int[] { 2, 3 }", "seed, rest", "int[];int[]", "[1];[2,3]", true, "int"];
        yield return ["T[] seed, params object[] rest", "(int[], int[])", "new int[] { 1 }, new int[] { 2, 3 }", "seed, rest", "int[];object", "[1];[[2,3]]", true, "int"];
        yield return ["T[] seed, params int[][] rest", "(int[], int[])", "new int[] { 1 }, new int[] { 2, 3 }", "seed, rest", "int[];int[]", "[1];[[2,3]]", true, "int"];
        yield return ["T[] seed, long[] rest", "(int[], long[])", "new int[] { 1 }, new long[] { 2, 3 }", "seed, rest", "int[];long[]", "[1];[2,3]", true, "int"];
        yield return ["T[] seed, params long[] rest", "(int[], int, int)", "new int[] { 1 }, 2, 3", "seed, rest", "int[];long;long", "[1];[2,3]", false, "int"];
        yield return ["CancellationToken token, T value, T[] seed", "(CancellationToken, CancellationToken[])", "CancellationToken.None, new CancellationToken[] { CancellationToken.None }", "token, value, seed", "System.Threading.CancellationToken;System.Threading.CancellationToken[]", "token;token;[token]", true, "System.Threading.CancellationToken"];
        yield return ["T value, CancellationToken token, T[] seed", "(CancellationToken, CancellationToken[])", "CancellationToken.None, new CancellationToken[] { CancellationToken.None }", "value, token, seed", "System.Threading.CancellationToken;System.Threading.CancellationToken[]", "token;token;[token]", true, "System.Threading.CancellationToken"];
#if NET9_0_OR_GREATER
        foreach (var collection in new[] { "IEnumerable", "ICollection", "IList", "IReadOnlyCollection", "IReadOnlyList" })
        {
            yield return [$"T[] seed, params {collection}<long> rest", "(int[], int, int)", "new int[] { 1 }, 2, 3", "seed, rest", "int[];long;long", "[1];[2,3]", false, "int"];
            yield return [$"T[] seed, params {collection}<long> rest", "int[]", "new int[] { 1 }", "seed, rest", "int[]", "[1];[]", true, "int"];
        }
#endif
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Expected_types_agree_with_generated_and_reflection_invocation(string parameters,
        string rowType, string data, string capture, string expectedTypes, string expectedValues,
        bool infers, string typeArgument)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading;
            public class Subject
            {
                public static object[] Result;
                public static void Run<T>({{parameters}}) { Result = new object[] { {{capture}} }; }
                public static {{rowType}} Row() => throw new Exception();
                public static object[] Data() => new object[] { {{data}} };
            }
            """;
        var compilation = Compile(source);
        var subject = compilation.GetTypeByMetadataName("Subject")!;
        var method = subject.GetMembers("Run").OfType<IMethodSymbol>().Single();
        var row = subject.GetMembers("Row").OfType<IMethodSymbol>().Single().ReturnType;
        var inference = typeof(TestMetadataGenerator).Assembly.GetType("TUnit.Analyzers.Extensions.TypedDataSourceInference")!;
        var inferred = (ITypeSymbol[]?)inference.GetMethod("InferMethodTypeArgumentsFromRowType")!.Invoke(null, [method, row]);
        await Assert.That(inferred is not null).IsEqualTo(infers);
        if (infers)
        {
            await Assert.That(inferred!.Single().ToDisplayString()).IsEqualTo(typeArgument);
        }
        var argument = typeArgument == "int" ? compilation.GetSpecialType(SpecialType.System_Int32)
            : compilation.GetTypeByMetadataName(typeArgument)!;
        var closed = method.Construct(argument);
        var rowArguments = (ImmutableArray<ITypeSymbol>)inference.GetMethod("GetRowArgumentTypes")!.Invoke(null, [row])!;
        var binding = typeof(TestDataAnalyzer).Assembly.GetType("TUnit.Analyzers.Extensions.TypedRowParameterBinding")!;
        var expected = (ImmutableArray<ITypeSymbol>)binding.GetMethod("GetExpectedTypes")!
            .Invoke(null, [compilation, method, closed, rowArguments])!;
        await Assert.That(string.Join(";", expected.Select(t => t.ToDisplayString()))).IsEqualTo(expectedTypes);

        var dataParameters = method.Parameters.Where(p => p.Type.ToDisplayString() != "System.Threading.CancellationToken")
            .Select(p => closed.Parameters[p.Ordinal]).ToArray();
        var calls = TupleArgumentHelper.GenerateArgumentAccessWithParams(dataParameters, "args", rowArguments.Length);
        // Named arguments let injected tokens stay in their declared positions after omitted defaults.
        var invocation = calls.Select((a, i) => a.IsNamed ? a.ToString() : $"@{dataParameters[i].Name}: {a.Expression}")
            .Concat(method.Parameters.Where(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken")
                .Select(p => $"@{p.Name}: global::System.Threading.CancellationToken.None"));
        var wrapper = $$"""
            public static class Generated
            {
                public static void Invoke(object[] args) => Subject.Run<{{typeArgument}}>({{string.Join(", ", invocation)}});
            }
            """;
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(wrapper, new CSharpParseOptions(LanguageVersion.Preview)));
        using var bytes = new MemoryStream();
        var emitted = compilation.Emit(bytes);
        await Assert.That(emitted.Success).IsTrue().Because(string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(bytes.ToArray());
        var runtimeSubject = assembly.GetType("Subject")!;
        var values = (object?[])runtimeSubject.GetMethod("Data")!.Invoke(null, null)!;
        assembly.GetType("Generated")!.GetMethod("Invoke")!.Invoke(null, [values]);
        await Assert.That(FormatResult(runtimeSubject)).IsEqualTo(expectedValues);

        var collector = typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionTestDataCollector")!;
        var reflected = (Type[]?)collector.GetMethod("InferTypesFromTypedRow", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [runtimeSubject.GetMethod("Run")!, runtimeSubject.GetMethod("Row")!.ReturnType, 1]);
        await Assert.That(reflected is not null).IsEqualTo(infers);
        if (infers)
        {
            await Assert.That(reflected!.Single()).IsEqualTo(typeArgument == "int" ? typeof(int) : typeof(CancellationToken));
        }
        var runtimeMethod = runtimeSubject.GetMethod("Run")!.MakeGenericMethod(typeArgument == "int" ? typeof(int) : typeof(CancellationToken));
        var invoker = (Func<object, object?[], Task>)collector.GetMethod("CreateReflectionTestInvoker", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [runtimeSubject, runtimeMethod])!;
        var withTokens = new List<object?>();
        var index = 0;
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.ToDisplayString() == "System.Threading.CancellationToken") withTokens.Add(CancellationToken.None);
            else if (index < values.Length) withTokens.Add(values[index++]);
        }
        withTokens.AddRange(values.Skip(index));
        await invoker(null!, withTokens.ToArray());
        await Assert.That(FormatResult(runtimeSubject)).IsEqualTo(expectedValues);
    }

    [Test]
    [Arguments("int[]", true)]
    [Arguments("IEnumerable<int>", true)]
    [Arguments("ICollection<int>", true)]
    [Arguments("IList<int>", true)]
    [Arguments("IReadOnlyCollection<int>", true)]
    [Arguments("IReadOnlyList<int>", true)]
    [Arguments("List<int>", false)]
    [Arguments("HashSet<int>", false)]
    [Arguments("Span<int>", false)]
    [Arguments("ReadOnlySpan<int>", false)]
    public async Task Collecting_element_types_match_both_invocation_implementations(string type, bool supported)
    {
        var compilation = Compile($"using System; using System.Collections.Generic; public class Subject {{ public void Run({type} values) {{ }} }}");
        var parameter = compilation.GetTypeByMetadataName("Subject")!.GetMembers("Run").OfType<IMethodSymbol>().Single().Parameters.Single();
        var analyzer = typeof(TestDataAnalyzer).Assembly.GetType("TUnit.Analyzers.Extensions.TypedRowParameterBinding")!
            .GetMethod("GetParamsElementType", BindingFlags.NonPublic | BindingFlags.Static)!;
        var generator = typeof(TupleArgumentHelper).GetMethod("GetTrailingElementType", BindingFlags.NonPublic | BindingFlags.Static)!;
        var analyzed = (ITypeSymbol?)analyzer.Invoke(null, [parameter.Type]);
        var generated = (ITypeSymbol?)generator.Invoke(null, [parameter.Type]);
        await Assert.That(analyzed?.ToDisplayString()).IsEqualTo(supported ? "int" : null);
        await Assert.That(generated?.ToDisplayString()).IsEqualTo(supported ? "int" : null);
        using var bytes = new MemoryStream();
        var emitted = compilation.Emit(bytes);
        await Assert.That(emitted.Success).IsTrue().Because(string.Join(Environment.NewLine, emitted.Diagnostics));
        var runtimeParameter = System.Reflection.Assembly.Load(bytes.ToArray()).GetType("Subject")!.GetMethod("Run")!.GetParameters().Single();
        var reflected = (Type?)typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionTestDataCollector")!
            .GetMethod("GetTrailingElementType", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [runtimeParameter]);
        await Assert.That(reflected).IsEqualTo(supported ? typeof(int) : null);
    }

    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "RowBinding_" + Guid.NewGuid().ToString("N"),
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
        ReferencesHelper.References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static string FormatResult(Type subject) => string.Join(";", ((object[])subject.GetField("Result")!.GetValue(null)!).Select(Format));
    private static string Format(object? value) => value switch
    {
        CancellationToken => "token",
        string text => text,
        IEnumerable values => "[" + string.Join(",", values.Cast<object?>().Select(Format)) + "]",
        _ => value?.ToString() ?? "null"
    };
}
