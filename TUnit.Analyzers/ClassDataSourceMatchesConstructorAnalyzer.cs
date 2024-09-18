﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassDataSourceMatchesConstructorAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoMethodFound, Rules.Argument_Count_Not_Matching_Parameter_Count, Rules.WrongArgumentTypeTestDataSource, Rules.NotIEnumerable);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var constructor = namedTypeSymbol.InstanceConstructors.FirstOrDefault();
        var parameters = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        
        foreach (var attributeData in namedTypeSymbol.GetAttributes())
        {
            Check(context, namedTypeSymbol, attributeData, parameters);
        }
    }

    private void Check(SymbolAnalysisContext context, INamedTypeSymbol namedTypeSymbol, AttributeData attributeData,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var attributeClass = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        if (attributeClass == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource)
        {
            var hasSpecifiedClass = attributeData.ConstructorArguments.Length > 1;

            var methodClass = hasSpecifiedClass
                ? attributeData.ConstructorArguments[0].Value as INamedTypeSymbol ?? namedTypeSymbol
                : namedTypeSymbol;
            var methodName = attributeData.ConstructorArguments[hasSpecifiedClass ? 1 : 0].Value as string ??
                             string.Empty;

            var dataSourceMethod = methodClass
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => !m.ReturnsVoid)
                .Where(m => m.Parameters.IsDefaultOrEmpty)
                .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                .FirstOrDefault(m => m.Name == methodName);

            if (dataSourceMethod is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.NoMethodFound, attributeData.GetLocation())
                );
                return;
            }
            
            if (!dataSourceMethod.ReturnType.IsEnumerable(context, out var innerType))
            {
                innerType = dataSourceMethod.ReturnType;
            }

            if (innerType.IsTupleType && innerType is INamedTypeSymbol namedInnerType)
            {
                var tupleTypes = namedInnerType.TupleUnderlyingType?.TypeArguments ?? namedInnerType.TypeArguments;

                for (var index = 0; index < tupleTypes.Length; index++)
                {
                    var tupleType = tupleTypes[index];
                    var parameterType = parameters[index].Type;

                    if (!context.Compilation.HasImplicitConversion(tupleType, parameterType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                                attributeData.GetLocation() ?? namedTypeSymbol.Locations.FirstOrDefault(),
                                tupleType, parameterType)
                        );
                    }
                }
            }

            if (SymbolEqualityComparer.Default.Equals(innerType, parameters.FirstOrDefault()?.Type))
            {
                return;
            }
        }
        else if (attributeClass == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource)
        {
            var type = attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(0) ??
                       (INamedTypeSymbol)attributeData.ConstructorArguments.First().Value!;

            var parameterType = parameters.FirstOrDefault()?.Type;
            
            if (parameters.Length != 1 || !context.Compilation.HasImplicitConversion(type, parameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                        attributeData.GetLocation() ?? namedTypeSymbol.Locations.FirstOrDefault(),
                        type, parameterType)
                );
            }
        }
    }
}