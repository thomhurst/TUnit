using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingPolyfillAnalyzer : ConcurrentDiagnosticAnalyzer
{
    private static readonly ImmutableArray<string> RequiredTypes =
        ImmutableArray.Create("System.Runtime.CompilerServices.ModuleInitializerAttribute");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MissingPolyfillPackage);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (var typeName in RequiredTypes)
        {
            if (context.Compilation.GetTypeByMetadataName(typeName) is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MissingPolyfillPackage, Location.None, typeName));
            }
        }
    }
}
