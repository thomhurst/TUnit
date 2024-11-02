using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.EqualityComparers;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestDataAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.WrongArgumentTypeTestData, 
            Rules.NoTestDataProvided, 
            Rules.MethodParameterBadNullability,
            Rules.WrongArgumentTypeTestData,
            Rules.MethodMustBeParameterless,
            Rules.MethodMustBeStatic,
            Rules.MethodMustBePublic,
            Rules.NoMethodFound,
            Rules.MethodMustReturnData,
            Rules.TooManyArgumentsInTestMethod,
            Rules.PropertyRequiredNotSet,
            Rules.MustHavePropertySetter);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }
    
    private void AnalyzeMethod(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.IsAbstract)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes();
        
        Analyze(context, attributes, methodSymbol.Parameters.WithoutCancellationTokenParameter().ToImmutableArray(), null, methodSymbol.ContainingType);
    }

    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        if (propertySymbol.IsAbstract)
        {
            return;
        }

        var attributes = propertySymbol.GetAttributes();

        Analyze(context, attributes, ImmutableArray<IParameterSymbol>.Empty, propertySymbol,
            propertySymbol.ContainingType);
    }


    private void AnalyzeClass(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        if (namedTypeSymbol.IsAbstract)
        {
            return;
        }

        if (!namedTypeSymbol.IsTestClass(context.Compilation))
        {
            return;
        }

        var attributes = namedTypeSymbol.GetAttributes();

        var parameters = namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ??
                         ImmutableArray<IParameterSymbol>.Empty;
        
        Analyze(context, attributes, parameters, null, namedTypeSymbol);
    }

    private void Analyze(SymbolAnalysisContext context, 
        ImmutableArray<AttributeData> attributes, 
        ImmutableArray<IParameterSymbol> parameters,
        IPropertySymbol? propertySymbol, 
        INamedTypeSymbol testClassType)
    {
        var types = GetTypes(parameters, propertySymbol);

        var dataAttributes = attributes.Where(x =>
                x.AttributeClass?.AllInterfaces.Contains(
                    context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IDataAttribute
                        .WithoutGlobalPrefix), SymbolEqualityComparer.Default
                ) == true)
            .ToImmutableArray();

        if (dataAttributes.IsDefaultOrEmpty)
        {
            return;
        }

        CheckPropertyAccessor(context, propertySymbol);
        
        foreach (var attribute in dataAttributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                    context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.Arguments.WithoutGlobalPrefix)))
            {
                CheckArguments(context, attribute, parameters);
            }
            
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                    context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.MethodDataSource.WithoutGlobalPrefix)))
            {
                CheckMethodDataSource(context, attribute, testClassType, types);
            }
            
            if (attribute.AttributeClass?.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IDataSourceGeneratorAttribute.WithoutGlobalPrefix))) == true)
            {
                CheckDataGenerator(context, attribute, types);
            }
        }

        CheckMatrix(context, parameters);
    }

    private void CheckPropertyAccessor(SymbolAnalysisContext context, IPropertySymbol? propertySymbol)
    {
        if (propertySymbol is null)
        {
            return;
        }
        
        if (propertySymbol is { IsStatic: false, IsRequired: false })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.PropertyRequiredNotSet, propertySymbol.Locations.FirstOrDefault()));
        }
            
        if (propertySymbol is { IsStatic: true, SetMethod: null })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MustHavePropertySetter, propertySymbol.Locations.FirstOrDefault()));
        }
    }

    private ImmutableArray<ITypeSymbol> GetTypes(ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? propertySymbol)
    {
        IEnumerable<ITypeSymbol?> types = [..parameters.Select(x => x.Type), propertySymbol?.Type];

        return types.OfType<ITypeSymbol>().ToImmutableArray();
    }

    private void CheckMatrix(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters)
    {
        if (!parameters.Any(x => x.HasMatrixAttribute(context.Compilation)))
        {
            return;
        }
        
        foreach (var parameterSymbol in parameters)
        {
            if (SymbolEqualityComparer.Default.Equals(parameters.LastOrDefault()?.Type,
                    context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
            {
                continue;
            }
            
            if (!parameterSymbol.HasMatrixAttribute(context.Compilation))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.NoTestDataProvided,
                        context.Symbol.Locations.FirstOrDefault())
                );
            }
        }
    }
    
    private void CheckArguments(SymbolAnalysisContext context, AttributeData argumentsAttribute,
        ImmutableArray<IParameterSymbol> parameters)
    {
        if (argumentsAttribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    argumentsAttribute.GetLocation())
            );
            return;
        }

        var arguments = argumentsAttribute.ConstructorArguments.First().IsNull
            ? ImmutableArray.Create(default(TypedConstant))
            : argumentsAttribute.ConstructorArguments.First().Values;

        var cancellationTokenType = context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);
        
        for (var i = 0; i < parameters.Length; i++)
        {
            var methodParameter = parameters[i];
            var argumentExists = i + 1 <= arguments.Length;
            var methodParameterType = methodParameter.Type;
            var argument = arguments.ElementAtOrDefault(i);
            
            if (SymbolEqualityComparer.Default.Equals(methodParameterType, cancellationTokenType))
            {
                continue;
            }

            if (!argumentExists && methodParameter.IsOptional)
            {
                continue;
            }

            if (!argumentExists)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation(),
                        "null",
                        methodParameterType.ToDisplayString())
                );
                return;
            }
            
            if (argument.IsNull && methodParameterType.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MethodParameterBadNullability,
                        parameters[i].Locations.FirstOrDefault(),
                        parameters[i].Name)
                );
            }
            
            if (IsEnumAndInteger(methodParameterType, argument.Type))
            {
                continue;
            }
            
            if (!argument.IsNull && !CanConvert(context, argument, methodParameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation(),
                        argument.Type?.ToDisplayString(),
                        methodParameterType.ToDisplayString())
                );
                return;
            }
        }
    }
    
    private void CheckMethodDataSource(SymbolAnalysisContext context, 
        AttributeData attribute,
        INamedTypeSymbol testClassType,
        ImmutableArray<ITypeSymbol> testDataParameterTypes)
    { 
        {
            var type = attribute.ConstructorArguments[0].Value as INamedTypeSymbol ?? testClassType;
            var methodName = attribute.ConstructorArguments[0].Value as string
                         ?? attribute.ConstructorArguments[1].Value as string;

            var argumentsNamedArgument = attribute.NamedArguments
                .FirstOrDefault(x => x.Key == "Arguments")
                .Value;

            var argumentTypes =
                argumentsNamedArgument.Kind == TypedConstantKind.Array
                    ? argumentsNamedArgument
                        .Values
                        .Select(x => x.Type)
                        .OfType<ITypeSymbol>()
                        .ToArray()
                    : [];

            var methodSymbols = type.GetSelfAndBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .ToArray();

            var methodContainingTestData = methodSymbols
                                               .FirstOrDefault(x =>
                                                   x.Name == methodName && x.Parameters.Select(p => p.Type)
                                                       .SequenceEqual(argumentTypes, new SelfOrBaseEqualityComparer(context.Compilation)))
                                           ?? methodSymbols.FirstOrDefault(x => x.Name == methodName);
            
            if (methodContainingTestData is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.NoMethodFound,
                        attribute.GetLocation())
                );
                return;
            }
        
            if (methodContainingTestData.ReturnsVoid)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustReturnData,
                        attribute.GetLocation())
                );
                return;
            }

            var canBeInstanceMethod = context.Symbol is IPropertySymbol;
            if (!canBeInstanceMethod && !methodContainingTestData.IsStatic && attribute.ConstructorArguments.Length != 1) 
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustBeStatic,
                        attribute.GetLocation())
                );
                return;
            }
        
            if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustBePublic,
                        attribute.GetLocation())
                );
                return;
            }
        
            if (context.Symbol is IPropertySymbol 
                || !methodContainingTestData.ReturnType.IsEnumerable(context, out var testDataMethodNonEnumerableReturnType))
            {
                testDataMethodNonEnumerableReturnType = methodContainingTestData.ReturnType;
            }

            var parameterTypes = methodContainingTestData.Parameters.Select(x => x.Type).ToArray();
                
            if (!parameterTypes.SequenceEqual(argumentTypes, new SelfOrBaseEqualityComparer(context.Compilation)))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join<ITypeSymbol>(", ", argumentTypes),
                        string.Join<ITypeSymbol>(", ", parameterTypes),
                        "for the `Arguments` array")
                );
                return;
            }

            if (testDataParameterTypes.Length == 1
                && context.Compilation.HasImplicitConversion(testDataMethodNonEnumerableReturnType,
                    testDataParameterTypes.FirstOrDefault()))
            {
                return;
            }

            if (testDataMethodNonEnumerableReturnType.IsGenericDefinition())
            {
                return;
            }

            if (testDataMethodNonEnumerableReturnType.IsTupleType)
            {
                var namedTypeSymbol = (INamedTypeSymbol) testDataMethodNonEnumerableReturnType;
            
                var returnTupleTypes = namedTypeSymbol.TupleUnderlyingType?.TypeArguments
                                       ?? namedTypeSymbol.TypeArguments;

                if (returnTupleTypes.Length != testDataParameterTypes.Length)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join(", ", returnTupleTypes),
                        string.Join(", ", testDataParameterTypes))
                    );
                    return;
                }
            
                for (var i = 0; i < testDataParameterTypes.Length; i++)
                {
                    var parameterType = testDataParameterTypes.ElementAtOrDefault(i);
                    var argumentType = returnTupleTypes.ElementAtOrDefault(i);

                    if (parameterType?.IsGenericDefinition() == true)
                    {
                        continue;
                    }
                
                    if (!context.Compilation.HasImplicitConversion(argumentType, parameterType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Rules.WrongArgumentTypeTestData,
                                attribute.GetLocation(),
                                argumentType,
                                parameterType)
                        );
                        return;
                    }
                }
            
                return;
            }
        
            if (testDataParameterTypes.Length > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.TooManyArgumentsInTestMethod,
                        attribute.GetLocation())
                );
                return;
            }
        
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestData,
                    attribute.GetLocation(),
                    testDataMethodNonEnumerableReturnType,
                    string.Join(", ", testDataParameterTypes))
            );
        }
    }
    
    private void CheckDataGenerator(SymbolAnalysisContext context, 
        AttributeData attribute,
        ImmutableArray<ITypeSymbol> testDataTypes)
    {
        var selfAndBaseTypes = attribute.AttributeClass?.GetSelfAndBaseTypes() ?? [];
            
        var baseGeneratorAttribute = selfAndBaseTypes
            .FirstOrDefault(x => x.Interfaces.Any(i => i.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IDataSourceGeneratorAttribute.WithGlobalPrefix));

        if (baseGeneratorAttribute is null)
        {
            return;
        }

        if (testDataTypes.Any(x => x.IsGenericDefinition()))
        {
            return;
        }

        if (testDataTypes.SequenceEqual(baseGeneratorAttribute.TypeArguments, new SelfOrBaseEqualityComparer(context.Compilation)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                attribute.GetLocation() ?? context.Symbol.Locations.FirstOrDefault(),
                string.Join(", ", baseGeneratorAttribute.TypeArguments), string.Join(", ", testDataTypes)));
    }

    private static bool CanConvert(SymbolAnalysisContext context, TypedConstant argument, ITypeSymbol? methodParameterType)
    {
        if (methodParameterType?.SpecialType == SpecialType.System_Decimal &&
            argument.Type?.SpecialType == SpecialType.System_Double &&
            decimal.TryParse(argument.Value?.ToString(), out _))
        {
            // Decimals can't technically be used in attributes, but we can still write it as a double
            // e.g. [Arguments(1.55)]
            return true;
        }
        
        return context.Compilation.HasImplicitConversion(argument.Type, methodParameterType);
    }

    private bool IsEnumAndInteger(ITypeSymbol? type1, ITypeSymbol? type2)
    {
        if (type1?.SpecialType is SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64)
        {
            return type2?.TypeKind == TypeKind.Enum;
        }
        
        if (type2?.SpecialType is SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64)
        {
            return type1?.TypeKind == TypeKind.Enum;
        }

        return false;
    }
}