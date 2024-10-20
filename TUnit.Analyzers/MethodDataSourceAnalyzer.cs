using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodDataSourceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.Argument_Count_Not_Matching_Parameter_Count,
            Rules.MethodMustReturnData,
            Rules.NoMethodFound,
            Rules.MethodMustBeStatic,
            Rules.MethodMustBePublic,
            Rules.MethodMustBeParameterless,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.TooManyArgumentsInTestMethod
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
                == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            return;
        }

        INamedTypeSymbol testClassType = null!;
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypeSymbols;
        if (context.Symbol is IMethodSymbol methodSymbol)
        {
            parameterOrPropertyTypeSymbols = methodSymbol.Parameters.Select(x => x.Type).ToImmutableArray().WithoutTimeoutParameter().ToImmutableArray();
            testClassType = methodSymbol.ContainingType;
        }
        else if (context.Symbol is INamedTypeSymbol namedTypeSymbol)
        {
            parameterOrPropertyTypeSymbols = namedTypeSymbol.Constructors.FirstOrDefault()?.Parameters.Select(x => x.Type).ToImmutableArray().WithoutTimeoutParameter().ToImmutableArray() ?? ImmutableArray<ITypeSymbol>.Empty;
            testClassType = namedTypeSymbol;
        } else if (context.Symbol is IPropertySymbol propertySymbol)
        {
            parameterOrPropertyTypeSymbols = ImmutableArray.Create(propertySymbol.Type);
            testClassType = propertySymbol.ContainingType;
        }

        if (!parameterOrPropertyTypeSymbols.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.Argument_Count_Not_Matching_Parameter_Count, context.Symbol.Locations.FirstOrDefault()));
            return;
        }
        
        foreach (var attributeData in context.Symbol.GetAttributes().Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                    == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            var type = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol ?? testClassType;
            var methodName = attributeData.ConstructorArguments[0].Value as string
                         ?? attributeData.ConstructorArguments[1].Value as string;

            var methodContainingTestData = type.GetSelfAndBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .FirstOrDefault(x => x.Name == methodName);
            
            if (methodContainingTestData is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoMethodFound,
                    attributeData.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustReturnData,
                    attributeData.GetLocation())
            );
            return;
        }

        var canBeInstanceMethod = context.Symbol is IPropertySymbol;
        if (!canBeInstanceMethod && !methodContainingTestData.IsStatic && attributeData.ConstructorArguments.Length != 1) 
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeStatic,
                    attributeData.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBePublic,
                    attributeData.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.Parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeParameterless,
                    attributeData.GetLocation())
            );
            return;
        }
        
        if (context.Symbol is IPropertySymbol 
            || !methodContainingTestData.ReturnType.IsEnumerable(context, out var testDataMethodNonEnumerableReturnType))
        {
            testDataMethodNonEnumerableReturnType = methodContainingTestData.ReturnType;
        }
        
        if (context.Compilation.HasImplicitConversion(testDataMethodNonEnumerableReturnType, parameterOrPropertyTypeSymbols.FirstOrDefault()))
        {
            return;
        }

        if (testDataMethodNonEnumerableReturnType.IsTupleType)
        {
            var namedTypeSymbol = (INamedTypeSymbol) testDataMethodNonEnumerableReturnType;
            
            var returnTupleTypes = namedTypeSymbol.TupleUnderlyingType?.TypeArguments
                                  ?? namedTypeSymbol.TypeArguments;

            if (returnTupleTypes.Length != parameterOrPropertyTypeSymbols.Length)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                        Rules.WrongArgumentTypeTestDataSource,
                        attributeData.GetLocation(),
                        string.Join(", ", returnTupleTypes),
                        string.Join(", ", parameterOrPropertyTypeSymbols))
                    );
                return;
            }
            
            for (var i = 0; i < parameterOrPropertyTypeSymbols.Length; i++)
            {
                var parameterType = parameterOrPropertyTypeSymbols.ElementAtOrDefault(i);
                var argumentType = returnTupleTypes.ElementAtOrDefault(i);

                if (parameterType is INamedTypeSymbol { IsGenericType: true })
                {
                    continue;
                }
                
                if (!context.Compilation.HasImplicitConversion(argumentType, parameterType))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rules.WrongArgumentTypeTestDataSource,
                            attributeData.GetLocation(),
                            argumentType,
                            parameterType)
                    );
                    return;
                }
            }
            
            return;
        }
        
        if (parameterOrPropertyTypeSymbols.Length > 1)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TooManyArgumentsInTestMethod,
                    attributeData.GetLocation())
            );
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestDataSource,
                    attributeData.GetLocation(),
                    testDataMethodNonEnumerableReturnType,
                    string.Join(", ", parameterOrPropertyTypeSymbols))
            );
        }
    }
}