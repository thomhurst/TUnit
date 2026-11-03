using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers;

namespace TUnit.Core.SourceGenerator.Tests;

// This host uses current Roslyn; the analyzer-test project intentionally hosts Roslyn 4.8,
// which predates C# 13 params collections.
internal class TypedArrayParamsAnalyzerTests
{
#if NET9_0_OR_GREATER
    [Test]
    [Arguments("IEnumerable", "string", false)]
    [Arguments("ICollection", "string", false)]
    [Arguments("IList", "string", false)]
    [Arguments("IReadOnlyCollection", "string", false)]
    [Arguments("IReadOnlyList", "string", false)]
    [Arguments("IEnumerable", "string[]", false)]
    [Arguments("IEnumerable", "int", true)]
    public async Task Closed_signature_validates_params_collection_elements(string collection, string suppliedType, bool invalid)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Core;
            public class ArrayRowAttribute : TypedDataSourceAttribute<(int[], int, {{suppliedType}})>
            {
                public override async IAsyncEnumerable<Func<Task<(int[], int, {{suppliedType}})>>> GetTypedDataRowsAsync(DataGeneratorMetadata metadata)
                {
                    await Task.Yield();
                    yield break;
                }
            }
            public class Subject
            {
                [Test, ArrayRow]
                public void Run<T>(T[] values, T value, params {{collection}}<string> rest) { }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "TypedArrayParams_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors.Length).IsEqualTo(0).Because(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));

        var diagnostics = await compilation.WithAnalyzers([new TestDataAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        await Assert.That(diagnostics.Length).IsEqualTo(invalid ? 1 : 0);
        if (invalid)
        {
            await Assert.That(diagnostics.Single().Id).IsEqualTo("TUnit0001");
            await Assert.That(diagnostics.Single().GetMessage()).Contains("int[], int, string");
        }
    }
#endif
}
