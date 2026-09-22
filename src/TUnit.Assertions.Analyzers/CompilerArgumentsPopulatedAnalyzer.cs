using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Reports explicitly passed arguments for TUnit.Assertions parameters that the compiler is meant to populate
/// (<c>[CallerMemberName]</c> / <c>[CallerArgumentExpression]</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CompilerArgumentsPopulatedAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.CompilerArgumentsPopulated);

    public override void InitializeInternal(AnalysisContext context)
    {
        // An operation action reuses the operation tree the driver already builds, instead of calling
        // SemanticModel.GetOperation for every ArgumentSyntax in the compilation.
        context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
    }

    private static void AnalyzeArgument(OperationAnalysisContext context)
    {
        if (context.Operation is not IArgumentOperation argumentOperation)
        {
            return;
        }

        // Only arguments written in source: SemanticModel.GetOperation(ArgumentSyntax) maps to the single
        // non-implicit operation for that syntax, so this matches exactly the arguments a syntax-node
        // action on SyntaxKind.Argument would see.
        if (argumentOperation.IsImplicit || argumentOperation.Syntax is not ArgumentSyntax argumentSyntax)
        {
            return;
        }

        if (argumentOperation.Parent?.Type?.ContainingAssembly?.Name is not "TUnit.Assertions")
        {
            return;
        }

        if (argumentOperation.Parameter?.GetAttributes().Any(x =>
                x.AttributeClass is { } attributeClass
                && (attributeClass.IsGloballyQualified("global::System.Runtime.CompilerServices.CallerMemberNameAttribute")
                    || attributeClass.IsGloballyQualified("global::System.Runtime.CompilerServices.CallerArgumentExpressionAttribute")))
            == true)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.CompilerArgumentsPopulated,
                    argumentSyntax.GetLocation())
            );
        }
    }
}
