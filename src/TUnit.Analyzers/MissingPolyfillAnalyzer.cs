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
            if (!HasType(context.Compilation, typeName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MissingPolyfillPackage, Location.None, typeName));
            }
        }
    }

    private static bool HasType(Compilation compilation, string typeName)
    {
        // GetTypeByMetadataName returns null when the type exists in multiple
        // referenced assemblies (e.g. when multiple libraries embed
        // ModuleInitializerAttribute via Polyfill). Check each assembly
        // individually to avoid this ambiguity problem.
        if (compilation.GetTypeByMetadataName(typeName) is not null)
        {
            return true;
        }

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly &&
                assembly.GetTypeByMetadataName(typeName) is not null)
            {
                return true;
            }
        }

        return false;
    }
}
