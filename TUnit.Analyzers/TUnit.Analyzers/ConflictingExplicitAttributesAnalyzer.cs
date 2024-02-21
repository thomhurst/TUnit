using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConflictingExplicitAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    private static readonly string[] TestAttributes = [ "global::TUnit.Core.TestAttribute", "global::TUnit.Core.DataDrivenTestAttribute", "global::TUnit.Core.DataSourceDrivenTestAttribute", "global::TUnit.Core.CombinativeTestAttribute"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConflictingExplicitAttributes);

    public override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax)
            is not { } methodSymbol)
        {
            return;
        }

        var methodExplicitAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == "global::TUnit.Core.ExplicitAttribute");

        if (methodExplicitAttribute == null)
        {
            return;
        }
        
        var classExplicitAttribute = methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.ExplicitAttribute");
        
        if (classExplicitAttribute == null)
        {
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(Rules.ConflictingExplicitAttributes,
                    methodExplicitAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                    ?? methodDeclarationSyntax.GetLocation())
            );
    }
}