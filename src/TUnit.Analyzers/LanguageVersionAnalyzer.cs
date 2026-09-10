using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

/// <summary>
/// Analyzer that checks the C# language version is compatible with TUnit.
/// TUnit requires C# 12 or higher when using source generation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LanguageVersionAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor CSharp12Required = new(
        id: "TUNIT_LANG_001",
        title: "Language Version Check",
        messageFormat: "TUnit requires C# 12 or higher when using Source Generation",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(CSharp12Required);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        if (context.Compilation is not CSharpCompilation csharpCompilation)
        {
            return;
        }

        var languageVersion = csharpCompilation.LanguageVersion;

        // C# 12 = LanguageVersion.CSharp12 = 1200
        if ((int)languageVersion < 1200)
        {
            context.ReportDiagnostic(Diagnostic.Create(CSharp12Required, Location.None));
        }
    }
}
