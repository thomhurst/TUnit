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
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (invocationOperation.TargetMethod.Name != "Equals")
        {
            return;
        }

        var instanceType = invocationOperation.Instance?.Type as INamedTypeSymbol;

        // Check if the instance implements IAssertionSource or inherits from Assertion<T>
        var isAssertionSource = instanceType?.AllInterfaces
            .Select(x => x.GloballyQualifiedNonGeneric())
            .Any(x => x is "global::TUnit.Assertions.Core.IAssertionSource") == true;

        var isAssertion = instanceType?.BaseType != null &&
            IsAssertionType(instanceType.BaseType);

        if (!isAssertionSource && !isAssertion)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.ObjectEqualsBaseMethod, invocationOperation.Syntax.GetLocation())
        );
    }

    private static bool IsAssertionType(INamedTypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        // Check if this type is Assertion<T>
        if (type.GloballyQualifiedNonGeneric() is "global::TUnit.Assertions.Core.Assertion")
        {
            return true;
        }

        // Check base type recursively
        return IsAssertionType(type.BaseType);
    }
}
