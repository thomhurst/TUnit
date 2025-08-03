using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Assertions.Analyzers;

public abstract class ConcurrentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public override sealed void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        InitializeInternal(context);
    }

    public abstract void InitializeInternal(AnalysisContext context);
}
