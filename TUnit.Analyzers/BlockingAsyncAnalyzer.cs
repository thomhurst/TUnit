using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BlockingAsyncAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.BlockingAsyncCall);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        
        if (memberAccess.Name.Identifier.Text == "Result")
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IPropertySymbol property &&
                property.ContainingType != null &&
                IsTaskType(property.ContainingType))
            {
                var diagnostic = Diagnostic.Create(
                    Rules.BlockingAsyncCall,
                    memberAccess.GetLocation(),
                    ".Result");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "GetResult" &&
            memberAccess.Expression is InvocationExpressionSyntax getAwaiterInvocation &&
            getAwaiterInvocation.Expression is MemberAccessExpressionSyntax getAwaiterAccess &&
            getAwaiterAccess.Name.Identifier.Text == "GetAwaiter")
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(getAwaiterAccess.Expression);
            if (symbolInfo.Symbol != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(getAwaiterAccess.Expression);
                if (typeInfo.Type != null && IsTaskType(typeInfo.Type))
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.BlockingAsyncCall,
                        invocation.GetLocation(),
                        "GetAwaiter().GetResult()");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsTaskType(ITypeSymbol type)
    {
        if (type == null)
        {
            return false;
        }

        var fullName = type.ToDisplayString();
        return fullName == "System.Threading.Tasks.Task" ||
               fullName.StartsWith("System.Threading.Tasks.Task<") ||
               fullName == "System.Threading.Tasks.ValueTask" ||
               fullName.StartsWith("System.Threading.Tasks.ValueTask<");
    }
}