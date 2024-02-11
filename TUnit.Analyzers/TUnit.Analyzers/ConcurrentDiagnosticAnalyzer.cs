using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

public abstract class ConcurrentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
            
        InitializeInternal(context);
    }

    public abstract void InitializeInternal(AnalysisContext context);
}