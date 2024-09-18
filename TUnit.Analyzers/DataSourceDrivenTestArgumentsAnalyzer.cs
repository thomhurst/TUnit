using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataSourceDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.NoTestDataSourceProvided,
            Rules.NoMethodFound,
            Rules.MethodMustBeStatic,
            Rules.MethodMustBePublic,
            Rules.MethodMustNotBeAbstract,
            Rules.MethodMustBeParameterless,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.MethodMustReturnData,
            Rules.NoArgumentInTestMethod,
            Rules.TooManyArgumentsInTestMethod,
            Rules.NotIEnumerable
        );

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
    }
    
    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var methodParameterTypes = methodSymbol.Parameters.IsDefaultOrEmpty
            ? []
            : methodSymbol.Parameters.ToList();
        
        if (methodSymbol.HasTimeoutAttribute(out _)
            && SymbolEqualityComparer.Default.Equals(methodParameterTypes.LastOrDefault()?.Type,
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            methodParameterTypes.RemoveAt(methodParameterTypes.Count - 1);
        }

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => 
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) 
                         == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            CheckAttributeAgainstMethod(context, methodParameterTypes.ToImmutableArray(), dataSourceDrivenAttribute, methodSymbol.ContainingType, true);
        }
    }
    
    private void AnalyzeClass(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var attributes = namedTypeSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                        == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            CheckAttributeAgainstMethod(context, namedTypeSymbol.Constructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty, dataSourceDrivenAttribute, namedTypeSymbol, false);
        }
    }

    private void CheckAttributeAgainstMethod(SymbolAnalysisContext context,
        ImmutableArray<IParameterSymbol> parameters,
        AttributeData dataSourceDrivenAttribute,
        INamedTypeSymbol fallbackClassToSearchForDataSourceIn,
        bool canBeInstanceMethod)
    {
        if (parameters.Length == 0)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoArgumentInTestMethod,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        var methodParameterTypes = parameters.Select(x => x.Type).ToList();
        
        var methodContainingTestData = FindMethodContainingTestData(context, dataSourceDrivenAttribute, fallbackClassToSearchForDataSourceIn);
        
        if (methodContainingTestData is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoMethodFound,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustReturnData,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (!canBeInstanceMethod && !methodContainingTestData.IsStatic && dataSourceDrivenAttribute.ConstructorArguments.Length != 1) 
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeStatic,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBePublic,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.Parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeParameterless,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (!methodContainingTestData.ReturnType.IsEnumerable(context, out var testDataMethodNonEnumerableReturnType))
        {
            testDataMethodNonEnumerableReturnType = methodContainingTestData.ReturnType;
        }
        
        if (context.Compilation.HasImplicitConversion(testDataMethodNonEnumerableReturnType, methodParameterTypes.FirstOrDefault()))
        {
            return;
        }

        if (testDataMethodNonEnumerableReturnType.IsTupleType)
        {
            var namedTypeSymbol = (INamedTypeSymbol) testDataMethodNonEnumerableReturnType;
            
            var returnTupleTypes = namedTypeSymbol.TupleUnderlyingType?.TypeArguments
                                  ?? namedTypeSymbol.TypeArguments;

            if (returnTupleTypes.Length != parameters.WithoutTimeoutParameter().Count())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                        Rules.WrongArgumentTypeTestDataSource,
                        dataSourceDrivenAttribute.GetLocation(),
                        string.Join(", ", returnTupleTypes.Select(x => x.ToDisplayString())),
                        string.Join(", ", parameters.Select(x => x.Type.ToDisplayString()))
                    )
                );
            }
            
            for (var i = 0; i < methodParameterTypes.Count; i++)
            {
                var parameterType = methodParameterTypes[i];
                var argumentType = returnTupleTypes[i];

                if (!context.Compilation.HasImplicitConversion(argumentType, parameterType))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rules.WrongArgumentTypeTestDataSource,
                            dataSourceDrivenAttribute.GetLocation(),
                            testDataMethodNonEnumerableReturnType,
                            FormatParametersToString(methodParameterTypes))
                    );
                }
            }
            
            return;
        }
        
        if (parameters.WithoutTimeoutParameter().Count() > 1)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TooManyArgumentsInTestMethod,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestDataSource,
                    dataSourceDrivenAttribute.GetLocation(),
                    testDataMethodNonEnumerableReturnType,
                    FormatParametersToString(methodParameterTypes))
            );
    }

    private string FormatParametersToString(List<ITypeSymbol> methodParameterTypes)
    {
        if (!methodParameterTypes.Any())
        {
            return "()";
        }

        if (methodParameterTypes.Count == 1)
        {
            return methodParameterTypes[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return
            $"({string.Join(", ", methodParameterTypes.Select(x => x.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))})";
    }

    private static ITypeSymbol CreateEnumerableOfType(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        return context.SemanticModel.Compilation
            .GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            .Construct(typeSymbol);
    }

    private IMethodSymbol? FindMethodContainingTestData(SymbolAnalysisContext context, AttributeData dataSourceDrivenAttribute,
        INamedTypeSymbol classContainingTest)
    {
        if (dataSourceDrivenAttribute.ConstructorArguments.Length == 1)
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments.First().Value as string;
            return classContainingTest.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
        else
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments[1].Value as string;

            var @class =
                dataSourceDrivenAttribute.ConstructorArguments[0].Value as INamedTypeSymbol ?? classContainingTest;
            
            return @class?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
    }
}