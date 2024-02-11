using System;
using System.Diagnostics;
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
            // Get stack trace for the exception with source file information
            var st = new StackTrace(e, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();
            throw new ApplicationException(
                $"""
                Line: {line}
                {e.StackTrace}
                """);
        }
    }

    public abstract void InitializeInternal(AnalysisContext context);
}