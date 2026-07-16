using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LanguageVersionAnalyzer : DiagnosticAnalyzer
{
    // C# 14 = 1400 following the Roslyn LanguageVersion enum pattern (C# 12 = 1200, C# 13 = 1300, etc.)
    private const int CSharp14 = 1400;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM004_RequiresCSharp14);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        if (context.Compilation is not CSharpCompilation csharpCompilation)
        {
            return;
        }

        var languageVersion = csharpCompilation.LanguageVersion;
        var numericVersion = (int)languageVersion;

        // LanguageVersion.Preview is a very high value (int.MaxValue - 2), so >= 1400 covers both C# 14+ and Preview
        if (numericVersion >= CSharp14)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM004_RequiresCSharp14,
                Location.None,
                languageVersion.ToDisplayString()
            )
        );
    }
}
