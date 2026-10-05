using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers.Internal.Tests;

public class CultureSensitiveFormattingAnalyzerTests
{
    [Test]
    [Arguments("_ = $\"{i}\";", "Interpolation")]
    [Arguments("_ = $\"{d}\";", "Interpolation")]
    [Arguments("_ = $\"{n}\";", "Interpolation")]
    [Arguments("_ = $\"{dt}\";", "Interpolation")]
    [Arguments("_ = \"a\" + i;", "String concatenation")]
    [Arguments("s += i;", "String concatenation")]
    [Arguments("_ = string.Concat(s, i);", "string.Concat")]
    [Arguments("_ = string.Join(\",\", new[] { i });", "string.Join")]
    [Arguments("sb.Append($\"{i}\");", "Interpolation")]
    [Arguments("sb.Append(i);", "StringBuilder.Append")]
    [Arguments("sb.Append(d);", "StringBuilder.Append")]
    [Arguments("sb.AppendLine($\"{i}\");", "Interpolation")]
    [Arguments("writer.Write(i);", "TextWriter.Write")]
    [Arguments("_ = i.ToString();", "ToString()")]
    [Arguments("_ = d.ToString(\"N2\");", "ToString()")]
    [Arguments("_ = int.Parse(s);", "Int32.Parse")]
    [Arguments("_ = double.TryParse(s, out _);", "Double.TryParse")]
    [Arguments("_ = string.Format(\"{0}\", i);", "string.Format")]
    [Arguments("_ = string.Format(\"{0} {1} {2}\", s, u, d);", "string.Format")]
    [Arguments("writer.Write(\"{0} {1}\", s, i);", "TextWriter.Write")]
    [Arguments("_ = Convert.ToString(i);", "Convert.ToString")]
    [Arguments("_ = Convert.ToDouble(s);", "Convert.ToDouble")]
    public async Task Reports_Culture_Sensitive_Conversion(string statement, string expectedConversion)
    {
        var diagnostics = await Analyze(statement);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(CultureSensitiveFormattingAnalyzer.DiagnosticId);
        await Assert.That(diagnostics[0].GetMessage()).StartsWith(expectedConversion + " converts");
    }

    [Test]
    [Arguments("_ = i.ToString(CultureInfo.InvariantCulture);")]
    [Arguments("_ = FormattableString.Invariant($\"{i} {d}\");")]
    [Arguments("_ = string.Format(CultureInfo.InvariantCulture, \"{0}\", i);")]
    [Arguments("_ = int.Parse(s, CultureInfo.InvariantCulture);")]
    [Arguments("_ = double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);")]
    [Arguments("_ = Convert.ToString(i, CultureInfo.InvariantCulture);")]
    [Arguments("_ = $\"{u} {b} {c} {e} {s}\";")]
    [Arguments("_ = $\"{i:X8}\";")]
    [Arguments("sb.Append(CultureInfo.InvariantCulture, $\"{i} {d}\");")]
    [Arguments("sb.Append($\"{u} {i:x}\");")]
    [Arguments("_ = \"a\" + u + c + b;")]
    [Arguments("sb.Append(',', i);")]
    [Arguments("sb.Insert(i, \"x\");")]
    [Arguments("sb.Append(u).Append(c).Append(s);")]
    [Arguments("s += u;")]
    [Arguments("_ = string.Format(\"{0} {1}\", s, u);")]
    [Arguments("_ = string.Join(\",\", new[] { u });")]
    [Arguments("_ = string.Concat(s, c);")]
    public async Task Ignores_Invariant_Or_Culture_Independent_Conversion(string statement)
    {
        var diagnostics = await Analyze(statement);

        await Assert.That(diagnostics).IsEmpty();
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string statement)
    {
        var source = $$"""
            using System;
            using System.Globalization;
            using System.IO;
            using System.Text;

            enum Kind { A }

            class C
            {
                void M(int i, double d, int? n, DateTime dt, uint u, bool b, char c, Kind e, string s, StringBuilder sb, TextWriter writer)
                {
                    {{statement}}
                }
            }
            """;

        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "Analyzed",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (compileErrors.Length > 0)
        {
            throw new InvalidOperationException("Test source does not compile: " + string.Join(Environment.NewLine, compileErrors.Select(e => e.ToString())));
        }

        return await compilation
            .WithAnalyzers([new CultureSensitiveFormattingAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}
