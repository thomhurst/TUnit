using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

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

        var parameters = methodSymbol.Parameters.IsDefaultOrEmpty
            ? []
            : methodSymbol.Parameters.ToList();

        if (!parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    methodDeclarationSyntax.GetLocation())
            );
            return;
        }
        
        if (methodSymbol.HasTimeoutAttribute(out _)
            && SymbolEqualityComparer.Default.Equals(parameters.LastOrDefault()?.Type,
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            parameters.RemoveAt(parameters.Count - 1);
        }
        
        foreach (var parameterSymbol in parameters)
        {
            var combinativeValueAttribute = parameterSymbol.GetAttributes().FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnown.AttributeFullyQualifiedClasses.CombinativeValues);

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