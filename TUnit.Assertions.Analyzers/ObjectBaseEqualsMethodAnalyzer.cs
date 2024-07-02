using System.Collections.Immutable;
using System.Linq;
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
public class ObjectBaseEqualsMethodAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ObjectEqualsBaseMethod);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }
    
    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        var operation = context.Operation;
        
        if (operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (invocationOperation.TargetMethod.Name != "Equals" || !SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ReceiverType, context.Compilation.GetSpecialType(SpecialType.System_Object)))
        {
            return;
        }

        if ((invocationOperation.Instance?.Type as INamedTypeSymbol)
            ?.GetSelfAndBaseTypes()
            .Select(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix))
            .Any(x => x is "global::TUnit.Assertions.AssertionBuilders.AssertionBuilder" or "global::TUnit.Assertions.Connector" or "global::TUnit.Assertions.AssertConditions.Operators.And" or "global::TUnit.Assertions.AssertConditions.Operators.Or") != true)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.ObjectEqualsBaseMethod, invocationOperation.Syntax.GetLocation())
        );
    }
}