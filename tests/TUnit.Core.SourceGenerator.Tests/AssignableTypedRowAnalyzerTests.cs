using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssignableTypedRowAnalyzerTests
{
    [Test]
    [Arguments("IEnumerable<T>", "int[]", false)]
    [Arguments("IEnumerable<T>", "List<int>", false)]
    [Arguments("RowBase<T>", "DerivedRow", false)]
    [Arguments("IMarker<T>", "DiamondRow", false)]
    [Arguments("Outer<T>.Base", "NestedDerived", false)]
    [Arguments("Outer<T>.GenericBase<T>", "NestedGenericDerived", false)]
    [Arguments("Outer<T>.Base", "NestedDerived", true)]
    [Arguments("IEnumerable<T>", "int[]", true)]
    public async Task Analyzer_validates_inferred_closed_signature(string parameter, string row, bool invalid)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Core;
            public class RowBase<T> { }
            public class DerivedRow : RowBase<int> { }
            public class Outer<T>
            {
                public class Base { }
                public class GenericBase<U> { }
            }
            public class NestedDerived : Outer<int>.Base { }
            public class NestedGenericDerived : Outer<int>.GenericBase<int> { }
            public interface IMarker<T> { }
            public interface ILeft : IMarker<int> { }
            public interface IRight : IMarker<int> { }
            public class DiamondRow : ILeft, IRight { }
            public class RowAttribute : TypedDataSourceAttribute<({{row}}, {{(invalid ? "string" : "int")}})>
            {
                public override async IAsyncEnumerable<Func<Task<({{row}}, {{(invalid ? "string" : "int")}})>>> GetTypedDataRowsAsync(DataGeneratorMetadata metadata)
                {
                    await Task.Yield();
                    yield break;
                }
            }
            public class Subject
            {
                [Test, Row]
                public void Run<T>({{parameter}} value, int other) { }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "AssignableRowAnalyzer_" + Guid.NewGuid().ToString("N"),
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
        }
    }
}
