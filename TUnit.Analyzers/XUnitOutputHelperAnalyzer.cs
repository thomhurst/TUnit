using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitOutputHelperAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitTestOutputHelper);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private void AnalyzeAssignment(OperationAnalysisContext context)
    {
        if (context.Operation is not IAssignmentOperation assignmentOperation
            || !IsTestOutputHelper(assignmentOperation.Target.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitTestOutputHelper, GetLocation<AssignmentExpressionSyntax>(assignmentOperation)));
    }

    private void AnalyzeParameter(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IParameterSymbol parameterSymbol
            || !IsTestOutputHelper(parameterSymbol.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitTestOutputHelper, GetLocation<ParameterSyntax>(parameterSymbol)));
    }

    private void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol fieldSymbol
            || !IsTestOutputHelper(fieldSymbol.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitTestOutputHelper, GetLocation<FieldDeclarationSyntax>(fieldSymbol)));
    }

    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol propertySymbol
            || !IsTestOutputHelper(propertySymbol.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitTestOutputHelper, GetLocation<PropertyDeclarationSyntax>(propertySymbol)));
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation
            || !IsTestOutputHelper(invocationOperation.TargetMethod.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitTestOutputHelper, GetLocation<InvocationExpressionSyntax>(invocationOperation)));
    }

    private static bool IsTestOutputHelper(ITypeSymbol? type)
    {
        return type?
                .WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                .GloballyQualified()
            is "global::Xunit.Abstractions.ITestOutputHelper"
            or "global::Xunit.ITestOutputHelper";
    }

    private Location? GetLocation<T>(ISymbol symbol) where T : SyntaxNode
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();

            while (syntax != null)
            {
                if (syntax is T syntaxNode)
                {
                    return syntaxNode.GetLocation();
                }

                syntax = syntax.Parent;
            }
        }

        return null;
    }
    
    private Location? GetLocation<T>(IOperation operation) where T : SyntaxNode
    {
        var syntax = operation.Syntax;

        while (syntax != null)
        {
            if (syntax is T syntaxNode)
            {
                return syntaxNode.GetLocation();
            }

            syntax = syntax.Parent;
        }

        return null;
    }
}