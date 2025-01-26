using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitValueTaskAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AwaitValueTaskInAssertThat);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }
    
    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        var methodSymbol = invocationOperation.TargetMethod;

        var fullyQualifiedNonGenericMethodName = methodSymbol.GloballyQualifiedNonGeneric();
        
        if (fullyQualifiedNonGenericMethodName is not "global::TUnit.Assertions.Assert.That")
        {
            return;
        }

        var funcArgumentOperation = invocationOperation.Arguments.First();
        
        var type = funcArgumentOperation.Parameter?.Type;

        var valueTask = context.Compilation.GetTypeByMetadataName(typeof(ValueTask).FullName!)!;
        var genericValueTask = context.Compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName!)!;
        
        if (type?.IsOrInherits(valueTask) is true || type?.OriginalDefinition?.IsOrInherits(genericValueTask) is true)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.AwaitValueTaskInAssertThat, context.Operation.Syntax.GetLocation())
            );
            
            return;
        }

        if (type is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }
        
        if(namedTypeSymbol.DelegateInvokeMethod?.ReturnType.IsOrInherits(valueTask) is not true 
           && namedTypeSymbol.DelegateInvokeMethod?.ReturnType.OriginalDefinition.IsOrInherits(genericValueTask) is not true)
        {
            return;
        }

        if (funcArgumentOperation.Descendants().Any(x => x.Kind == OperationKind.Await))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitValueTaskInAssertThat, context.Operation.Syntax.GetLocation())
        );
    }
}