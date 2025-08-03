using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

public abstract class ConcurrentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public override sealed void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        InitializeInternal(context);
    }

    protected abstract void InitializeInternal(AnalysisContext context);
}
