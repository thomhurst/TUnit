﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
            Rules.MethodMustBeParameterless,
            Rules.MethodMustBeStatic,
            Rules.MethodMustBePublic,
            Rules.NoMethodFound,
            Rules.MethodMustReturnData,
            Rules.TooManyArgumentsInTestMethod,
            Rules.PropertyRequiredNotSet,
            Rules.MustHavePropertySetter,
            Rules.ReturnFunc);

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
            
            if (attribute.AttributeClass?.IsGenericType is true 
                && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass.OriginalDefinition,
                    context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.GenericMethodDataSource.WithoutGlobalPrefix)))
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
        ImmutableArray<ITypeSymbol> testParameterTypes)
    {
        {
            var type = attribute.AttributeClass?.IsGenericType == true
                ? attribute.AttributeClass.TypeArguments.First()
                : attribute.ConstructorArguments[0].Value as INamedTypeSymbol ?? testClassType;
            
            var methodName = attribute.ConstructorArguments[0].Value as string
                         ?? attribute.ConstructorArguments[1].Value as string;

            var argumentsNamedArgument = attribute.NamedArguments
                .FirstOrDefault(x => x.Key == "Arguments")
                .Value;

            var argumentForMethodCallTypes =
                argumentsNamedArgument.Kind == TypedConstantKind.Array
                    ? argumentsNamedArgument
                        .Values
                        .Select(x => x.Type)
                        .OfType<ITypeSymbol>()
                        .ToArray()
                    : [];

            var methodSymbols = (type as INamedTypeSymbol)?.GetSelfAndBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .ToArray() ?? [];

            var dataSourceMethod = methodSymbols
                                               .FirstOrDefault(methodSymbol =>
                                                   methodSymbol.Name == methodName &&
                                                   MatchesParameters(context, argumentForMethodCallTypes, methodSymbol))
                                           ?? methodSymbols.FirstOrDefault(x => x.Name == methodName);
            
            if (dataSourceMethod is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.NoMethodFound,
                        attribute.GetLocation())
                );
                return;
            }
        
            if (dataSourceMethod.ReturnsVoid)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustReturnData,
                        attribute.GetLocation())
                );
                return;
            }

            var canBeInstanceMethod = context.Symbol is IPropertySymbol or IMethodSymbol
                && testClassType.InstanceConstructors.First().Parameters.IsDefaultOrEmpty;
            
            if (!canBeInstanceMethod && !dataSourceMethod.IsStatic && attribute.ConstructorArguments.Length != 1) 
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustBeStatic,
                        attribute.GetLocation())
                );
                return;
            }
        
            if (dataSourceMethod.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.MethodMustBePublic,
                        attribute.GetLocation())
                );
                return;
            }

            var unwrappedTypes = UnwrapTypes(context,
                dataSourceMethod,
                testParameterTypes,
                out var isEnumerable,
                out var isFunc,
                out var isTuples);

            if (!isFunc && unwrappedTypes.Any(x => x.SpecialType != SpecialType.System_String && x.IsReferenceType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ReturnFunc,
                    dataSourceMethod.Locations.FirstOrDefault()));
            }
            
            var dataSourceMethodParameterTypes = dataSourceMethod.Parameters.Select(x => x.Type).ToArray();

            if (!isTuples && testParameterTypes.Length > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.TooManyArgumentsInTestMethod,
                        attribute.GetLocation())
                );
                return;
            }

            var conversions = unwrappedTypes.ZipAll(testParameterTypes,
                (argument, parameter) => context.Compilation.HasImplicitConversionOrGenericParameter(argument, parameter));
            
            if (!conversions.All(x => x))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join(", ", unwrappedTypes),
                        string.Join<ITypeSymbol>(", ", testParameterTypes))
                );
                return;
            }

            var argumentsToParamsConversions = argumentForMethodCallTypes.ZipAll(dataSourceMethodParameterTypes,
                (argument, parameterType) => context.Compilation.HasImplicitConversionOrGenericParameter(argument, parameterType));
            
            if (!argumentsToParamsConversions.All(x => x))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join<ITypeSymbol>(", ", argumentForMethodCallTypes),
                        string.Join<ITypeSymbol>(", ", dataSourceMethodParameterTypes),
                        "for the `Arguments` array")
                );
                return;
            }

            if (testParameterTypes.Length == 1
                && unwrappedTypes.Length == 1
                && context.Compilation.HasImplicitConversionOrGenericParameter(unwrappedTypes[0],
                    testParameterTypes[0]))
            {
                return;
            }

            if (unwrappedTypes.Any(x => x.IsGenericDefinition()))
            {
                return;
            }

            if (isTuples)
            {
                if (unwrappedTypes.Length != testParameterTypes.Length)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join(", ", unwrappedTypes),
                        string.Join(", ", testParameterTypes))
                    );
                    return;
                }
            
                for (var i = 0; i < testParameterTypes.Length; i++)
                {
                    var parameterType = testParameterTypes.ElementAtOrDefault(i);
                    var argumentType = unwrappedTypes.ElementAtOrDefault(i);

                    if (parameterType?.IsGenericDefinition() == true)
                    {
                        continue;
                    }
                
                    if (!context.Compilation.HasImplicitConversionOrGenericParameter(argumentType, parameterType))
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
        
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestData,
                    attribute.GetLocation(),
                    string.Join(", ", unwrappedTypes),
                    string.Join(", ", testParameterTypes))
            );
        }
    }

    private static bool MatchesParameters(SymbolAnalysisContext context, ITypeSymbol[] argumentForMethodCallTypes, IMethodSymbol methodSymbol)
    {
        return argumentForMethodCallTypes.ZipAll(methodSymbol.Parameters.Select(p => p.Type), 
                (argument, parameter) => context.Compilation.HasImplicitConversionOrGenericParameter(argument, parameter))
            .All(x => x);
    }

    private ImmutableArray<ITypeSymbol> UnwrapTypes(SymbolAnalysisContext context,
        IMethodSymbol methodContainingTestData,
        ImmutableArray<ITypeSymbol> testParameterTypes,
        out bool isEnumerable,
        out bool isFunc,
        out bool isTuples)
    {
        isEnumerable = false;
        isFunc = false;
        isTuples = false;

        var type = methodContainingTestData.ReturnType;

        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return ImmutableArray.Create(type);
        }
        
        if (context.Symbol is IPropertySymbol)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } propertyFuncType
                && SymbolEqualityComparer.Default.Equals(
                    context.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName()),
                    type.OriginalDefinition))
            {
                isFunc = true;
                type = propertyFuncType.TypeArguments[0];
            }
            
            return ImmutableArray.Create(type);
        }
        
        if (methodContainingTestData.ReturnType is not INamedTypeSymbol namedTypeSymbol)
        {
            return ImmutableArray.Create(methodContainingTestData.ReturnType);
        }
        
        if (namedTypeSymbol.IsGenericType
            && SymbolEqualityComparer.Default.Equals(
                context.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T),
                namedTypeSymbol.OriginalDefinition))
        {
            isEnumerable = true;
            type = namedTypeSymbol.TypeArguments[0];
        }
        
        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return ImmutableArray.Create(type);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } genericType
            && SymbolEqualityComparer.Default.Equals(
                context.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName()),
                type.OriginalDefinition))
        {
            isFunc = true;
            type = genericType.TypeArguments[0];
        }
        
        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return ImmutableArray.Create(type);
        }

        if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            isTuples = true;
            return tupleType.TupleUnderlyingType?.TypeArguments ?? tupleType.TypeArguments;
        }
        
        return ImmutableArray.Create(type);
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

        var conversions = baseGeneratorAttribute.TypeArguments.ZipAll(testDataTypes,
            (returnType, parameterType) => context.Compilation.HasImplicitConversionOrGenericParameter(returnType, parameterType));
        
        if (conversions.All(x => x))
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
        
        return context.Compilation.HasImplicitConversionOrGenericParameter(argument.Type, methodParameterType);
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