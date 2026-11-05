using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableClassMethodSourceParityTests
{
    [Test]
    [Arguments("T? value, int required", "int", false)]
    [Arguments("T? value, int optional = 17", "int", true)]
    [Arguments("T? value, System.Threading.CancellationToken token, int required", "int", false)]
    [Arguments("T? value, System.Threading.CancellationToken token, int optional = 17", "int", true)]
    [Arguments("T? value, int required", "Func<int>", false)]
    [Arguments("T? value, int required", "(int, int)", true)]
    public async Task Required_trailing_parameters_do_not_infer_classes_from_scalar_rows(
        string parameters, string row, bool shouldInfer)
    {
        // Use the same declaration for generated discovery and reflection inference, including
        // optional-parameter and engine-supplied CancellationToken metadata.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class Subject<T> where T : struct
            {
                public static IEnumerable<{{row}}> Rows() => [];
                [Test, MethodDataSource(nameof(Rows))]
                public void Run({{parameters}}) { }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "NullableClassSourceParity_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var assemblyBytes = new MemoryStream();
        var emitted = compilation.Emit(assemblyBytes);
        await Assert.That(emitted.Success).IsTrue()
            .Because(string.Join(Environment.NewLine, emitted.Diagnostics));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMetadataGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        var result = driver.RunGenerators(compilation).GetRunResult();
        await Assert.That(result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        var generatesClass = result.GeneratedTrees.Any(tree => tree.ToString().Contains("global::Subject<int>"));
        await Assert.That(generatesClass).IsEqualTo(shouldInfer);

        var runtimeSubject = System.Reflection.Assembly.Load(assemblyBytes.ToArray()).GetType("Subject`1", throwOnError: true)!;
        var reflectionInference = typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionTestDataCollector", throwOnError: true)!
            .GetMethod("InferClassTypesFromMethodDataSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        var reflected = (Type[]?)reflectionInference.Invoke(null,
            [runtimeSubject, new MethodDataSourceAttribute("Rows"),
                runtimeSubject.GetMethod("Run")!.GetParameters(), runtimeSubject.GetGenericArguments()]);
        await Assert.That(reflected?.Single()).IsEqualTo(shouldInfer ? typeof(int) : null);
    }
}
