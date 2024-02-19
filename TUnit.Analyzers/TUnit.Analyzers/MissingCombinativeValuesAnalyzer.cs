using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingCombinativeValuesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoTestDataProvided);

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

        if (!methodSymbol.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == "global::TUnit.Core.CombinativeTestAttribute"))
        {
            return;
        }

        var parameters = methodSymbol.Parameters;

        if (parameters.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    methodDeclarationSyntax.GetLocation())
            );
            return;
        }
        
        foreach (var parameterSymbol in parameters)
        {
            var combinativeValueAttribute = parameterSymbol.GetAttributes().FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == "global::TUnit.Core.CombinativeValuesAttribute");

            if (combinativeValueAttribute is null
                || combinativeValueAttribute.ConstructorArguments.IsDefaultOrEmpty
                || combinativeValueAttribute.ConstructorArguments[0].Values.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.NoTestDataProvided,
                        parameterSymbol.Locations.FirstOrDefault())
                );
            }
        }
    }
}