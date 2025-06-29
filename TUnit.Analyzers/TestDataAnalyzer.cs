using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestDataAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
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
            Rules.ReturnFunc,
            Rules.MatrixDataSourceAttributeRequired,
            Rules.TooManyArguments,
            Rules.InstanceMethodSource
    ];

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

        Analyze(context, attributes, [
            ..methodSymbol.Parameters.WithoutCancellationTokenParameter()
        ], null, methodSymbol.ContainingType);
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
                CheckArguments(context, attribute, parameters, propertySymbol);
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

            if (attribute.AttributeClass?.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IAsyncDataSourceGeneratorAttribute.WithoutGlobalPrefix))) == true
                && attribute.AttributeClass?.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IAsyncUntypedDataSourceGeneratorAttribute.WithoutGlobalPrefix))) != true)
            {
                CheckDataGenerator(context, attribute, types);
            }
        }
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
        IEnumerable<ITypeSymbol?> types = [.. parameters.Select(x => x.Type), propertySymbol?.Type];

        return types.OfType<ITypeSymbol>().ToImmutableArray().WithoutCancellationTokenParameter();
    }

    private void CheckArguments(SymbolAnalysisContext context, AttributeData argumentsAttribute,
        ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? propertySymbol)
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
            ? [default(TypedConstant)]
            : argumentsAttribute.ConstructorArguments.First().Values;

        var cancellationTokenType = context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);

        for (var i = 0; i < Math.Max(parameters.Length, arguments.Length); i++)
        {
            var parameter = parameters.ElementAtOrDefault(i);

            if (parameter is null && propertySymbol is null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.TooManyArguments, argumentsAttribute.GetLocation())
                );
                break;
            }

            var argumentExists = i + 1 <= arguments.Length;

            var typeSymbol = parameter?.Type ?? propertySymbol!.Type;

            var argument = arguments.ElementAtOrDefault(i);

            if (typeSymbol.IsCollectionType(context.Compilation, out var innerType)
                && arguments.Skip(i).Select(x => x.Type).All(x => CanConvert(context, x, innerType)))
            {
                break;
            }

            if (SymbolEqualityComparer.Default.Equals(typeSymbol, cancellationTokenType))
            {
                continue;
            }

            if (!argumentExists && parameter?.IsOptional is true)
            {
                continue;
            }

            if (!argumentExists)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation(),
                        "null",
                        typeSymbol.ToDisplayString())
                );
                return;
            }

            if (argument.IsNull && typeSymbol.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MethodParameterBadNullability,
                        parameter?.Locations.FirstOrDefault() ?? propertySymbol!.Locations.FirstOrDefault(),
                        parameter?.Name ?? propertySymbol!.Name)
                );
            }

            if (IsEnumAndInteger(typeSymbol, argument.Type))
            {
                continue;
            }

            if (!argument.IsNull && !CanConvert(context, argument, typeSymbol))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation(),
                        argument.Type?.ToDisplayString(),
                        typeSymbol.ToDisplayString())
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

            if (!dataSourceMethod.IsStatic
                && !dataSourceMethod.ContainingType.InstanceConstructors.Any(c => c.Parameters.IsDefaultOrEmpty)
                && SymbolEqualityComparer.Default.Equals(dataSourceMethod.ContainingType, testClassType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.InstanceMethodSource,
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
                out var isFunc,
                out var isTuples);

            if (!isFunc && unwrappedTypes.Any(x => x.SpecialType != SpecialType.System_String && x.IsReferenceType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ReturnFunc,
                    dataSourceMethod.Locations.FirstOrDefault()));
            }

            var dataSourceMethodParameterTypes = dataSourceMethod.Parameters.Select(x => x.Type).ToArray();

            // Note: We no longer check if test has multiple parameters when data source doesn't return tuples
            // because the data source method can return arrays of arrays (object[][]) to satisfy multiple parameters

            // Skip type checking if unwrappedTypes is empty (indicating object[] which can contain any types)
            if (unwrappedTypes.Length == 0)
            {
                // object[] can contain any types - skip compile-time type checking
                return;
            }
            else
            {
                var conversions = unwrappedTypes.ZipAll(testParameterTypes,
                    (argument, parameter) => context.Compilation.HasImplicitConversionOrGenericParameter(argument, parameter));

                if (!conversions.All(x => x))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rules.WrongArgumentTypeTestData,
                            attribute.GetLocation(),
                            string.Join(", ", unwrappedTypes),
                            string.Join(", ", testParameterTypes))
                    );
                    return;
                }
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
        out bool isFunc,
        out bool isTuples)
    {
        isFunc = false;
        isTuples = false;

        var type = methodContainingTestData.ReturnType;

        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return [type];
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

            return [type];
        }

        if (methodContainingTestData.ReturnType is not INamedTypeSymbol and not IArrayTypeSymbol)
        {
            return [methodContainingTestData.ReturnType];
        }

        if (methodContainingTestData.ReturnType.IsIEnumerable(context.Compilation, out var enumerableInnerType))
        {
            type = enumerableInnerType;
        }

        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return [type];
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
            return [type];
        }

        if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            isTuples = true;
            return [
                ..tupleType.TupleElements.Select(x => x.Type)
            ];
        }

        // Handle object[] case - when a data source returns IEnumerable<object[]>,
        // each object[] contains the arguments for one test invocation
        if (type is IArrayTypeSymbol arrayType &&
            arrayType.ElementType.SpecialType == SpecialType.System_Object)
        {
            // Return empty array to indicate that type checking should be skipped
            // since object[] can contain any types that will be checked at runtime
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        return [type];
    }

    private void CheckDataGenerator(SymbolAnalysisContext context,
        AttributeData attribute,
        ImmutableArray<ITypeSymbol> testDataTypes)
    {
        var selfAndBaseTypes = attribute.AttributeClass?.GetSelfAndBaseTypes() ?? [];

        var baseGeneratorAttribute = selfAndBaseTypes
            .FirstOrDefault(x => x.Interfaces.Any(i => i.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IAsyncDataSourceGeneratorAttribute.WithGlobalPrefix));

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

        return CanConvert(context, argument.Type, methodParameterType);
    }

    private static bool CanConvert(SymbolAnalysisContext context, ITypeSymbol? argumentType, ITypeSymbol? methodParameterType)
    {
        if (methodParameterType is ITypeParameterSymbol)
        {
            return true;
        }

        if (argumentType is not null
            && methodParameterType is not null
            && context.Compilation.ClassifyConversion(argumentType, methodParameterType)
                is { IsImplicit: true }
                or { IsExplicit: true }
                or { IsNumeric: true })
        {
            return true;
        }

        return context.Compilation.HasImplicitConversionOrGenericParameter(argumentType, methodParameterType);
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
