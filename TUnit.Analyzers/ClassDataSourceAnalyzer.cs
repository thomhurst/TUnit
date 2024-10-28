using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassDataSourceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.TypeMustBePublic,
            Rules.Argument_Count_Not_Matching_Parameter_Count,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.NoMatchingParameterClassDataSource,
            Rules.ConstructorMustBeParameterless,
            Rules.PropertyRequiredNotSet,
            Rules.MustHavePropertySetter
        );

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not INamedTypeSymbol and not IMethodSymbol and not IPropertySymbol)
        {
            return;
        }

        if (!context.Symbol.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource.WithGlobalPrefix))
        {
            return;
        }
        
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypeSymbols;
        if (context.Symbol is IMethodSymbol methodSymbol)
        {
            parameterOrPropertyTypeSymbols = methodSymbol.Parameters.Select(x => x.Type).ToImmutableArray();
        }
        else if (context.Symbol is INamedTypeSymbol namedTypeSymbol)
        {
            parameterOrPropertyTypeSymbols = namedTypeSymbol.Constructors.FirstOrDefault()?.Parameters.Select(x => x.Type).ToImmutableArray() ?? ImmutableArray<ITypeSymbol>.Empty;
        } else if (context.Symbol is IPropertySymbol propertySymbol)
        {
            if (propertySymbol is { IsStatic: false, IsRequired: false })
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.PropertyRequiredNotSet, propertySymbol.Locations.FirstOrDefault()));
                return;
            }
            
            if (propertySymbol is { IsStatic: true, SetMethod: null })
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.MustHavePropertySetter, propertySymbol.Locations.FirstOrDefault()));
                return;
            }
            
            parameterOrPropertyTypeSymbols = ImmutableArray.Create(propertySymbol.Type);
        }

        if (!parameterOrPropertyTypeSymbols.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NoMatchingParameterClassDataSource, context.Symbol.Locations.FirstOrDefault()));
            return;
        }
        
        foreach (var attributeData in context.Symbol.GetAttributes().Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                    == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource.WithGlobalPrefix))
        {
            var type = attributeData.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol
                       ?? attributeData.AttributeClass!.TypeArguments.First();

            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.TypeMustBePublic, attributeData.GetLocation()));
                return;
            }
            
            if (type is INamedTypeSymbol namedTypeSymbol && !namedTypeSymbol.InstanceConstructors.Any(x => x.Parameters.Length == 0))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ConstructorMustBeParameterless, attributeData.GetLocation()));
                return;
            }
            
            if (!parameterOrPropertyTypeSymbols.Any(parameterOrPropertyTypes => context.Compilation.HasImplicitConversion(type, parameterOrPropertyTypes)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMatchingParameterClassDataSource, attributeData.GetLocation()));
            }
        }
    }
}