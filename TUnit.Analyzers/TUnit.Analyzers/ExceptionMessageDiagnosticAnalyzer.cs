using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

public abstract class ExceptionMessageDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public sealed override void Initialize(AnalysisContext context)
    {
        try
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            InitializeInternal(context);
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }
    }

    public abstract void InitializeInternal(AnalysisContext context);
}