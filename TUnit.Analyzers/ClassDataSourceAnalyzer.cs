using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassDataSourceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoMethodFound, Rules.Argument_Count_Not_Matching_Parameter_Count, Rules.WrongArgumentTypeTestDataSource, Rules.NoMatchingParameterClassDataSource);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not INamedTypeSymbol and not IMethodSymbol)
        {
            return;
        }

        if (!context.Symbol.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource))
        {
            return;
        }
        
        ImmutableArray<IParameterSymbol> parameterSymbols;
        if (context.Symbol is IMethodSymbol methodSymbol)
        {
            parameterSymbols = methodSymbol.Parameters;
        }
        else if (context.Symbol is INamedTypeSymbol namedTypeSymbol)
        {
            parameterSymbols = namedTypeSymbol.Constructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        }

        if (!parameterSymbols.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NoMatchingParameterClassDataSource, context.Symbol.Locations.FirstOrDefault()));
            return;
        }
        
        foreach (var attributeData in context.Symbol.GetAttributes().Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                    == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource))
        {
            var type = attributeData.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol
                       ?? attributeData.AttributeClass!.TypeArguments.First();
            
            if (!parameterSymbols.Any(parameter => context.Compilation.HasImplicitConversion(type, parameter.Type)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMatchingParameterClassDataSource, context.Symbol.Locations.FirstOrDefault()));
            }
        }
    }
}