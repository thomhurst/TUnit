using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
            Rules.ReturnFunc,
            Rules.MatrixDataSourceAttributeRequired,
            Rules.TooManyArguments,
            Rules.InstanceMethodSource
        );

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

        // Check if it's a test class or has data source attributes
        var hasDataSourceAttribute = namedTypeSymbol.GetAttributes().Any(a => 
        {
            var currentType = a.AttributeClass;
            while (currentType != null)
            {
                if (currentType.Name.Contains("DataSource") || currentType.Name == "ArgumentsAttribute")
                {
                    return true;
                }
                currentType = currentType.BaseType;
            }
            return false;
        });
        
        if (!namedTypeSymbol.IsTestClass(context.Compilation) && !hasDataSourceAttribute)
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

        var dataSourceInterface = context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IDataSourceAttribute.WithoutGlobalPrefix);
        
        var dataAttributes = attributes.Where(x =>
            {
                if (x.AttributeClass == null)
                {
                    return false;
                }

                var attributeClass = x.AttributeClass;
                
                // Check if this is a known data source attribute by inheritance chain
                var currentType = attributeClass;
                while (currentType != null)
                {
                    var typeName = currentType.Name;
                    
                    // Check for known data source attributes
                    if (typeName == "ArgumentsAttribute")
                    {
                        return true;
                    }
                    
                    // For generic types, check the type name without arity
                    if (currentType.IsGenericType)
                    {
                        var genericTypeName = currentType.OriginalDefinition?.Name ?? typeName;
                        if (genericTypeName.StartsWith("DataSourceGeneratorAttribute") ||
                            genericTypeName.StartsWith("AsyncDataSourceGeneratorAttribute") ||
                            genericTypeName.StartsWith("ClassDataSourceAttribute"))
                        {
                            return true;
                        }
                    }
                    
                    // Also check if we have IDataSourceAttribute interface
                    if (dataSourceInterface != null && 
                        currentType.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, dataSourceInterface)))
                    {
                        return true;
                    }
                    
                    currentType = currentType.BaseType;
                }
                
                return false;
            })
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
                // For property injection, only validate against the property type, not method parameters
                var typesToValidate = propertySymbol != null 
                    ? ImmutableArray.Create(propertySymbol.Type)
                    : parameters.Select(p => p.Type).ToImmutableArray().WithoutCancellationTokenParameter();
                CheckMethodDataSource(context, attribute, testClassType, typesToValidate, propertySymbol);
            }

            if (attribute.AttributeClass?.IsGenericType is true
                && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass.OriginalDefinition,
                    context.Compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.GenericMethodDataSource.WithoutGlobalPrefix)))
            {
                // For property injection, only validate against the property type, not method parameters
                var typesToValidate = propertySymbol != null 
                    ? ImmutableArray.Create(propertySymbol.Type)
                    : parameters.Select(p => p.Type).ToImmutableArray().WithoutCancellationTokenParameter();
                CheckMethodDataSource(context, attribute, testClassType, typesToValidate, propertySymbol);
            }
            
            // Check for ClassDataSourceAttribute<T> by fully qualified name
            if (attribute.AttributeClass?.ToDisplayString()?.StartsWith("TUnit.Core.ClassDataSourceAttribute<") == true)
            {
                var typesToValidate = propertySymbol != null 
                    ? ImmutableArray.Create(propertySymbol.Type)
                    : types;
                CheckDataGenerator(context, attribute, typesToValidate);
            }
            
            // Check for any custom data source generators that inherit from known base classes
            // (excluding ClassDataSourceAttribute which is handled above)
            if (attribute.AttributeClass != null && 
                !attribute.AttributeClass.ToDisplayString()?.StartsWith("TUnit.Core.ClassDataSourceAttribute<") == true)
            {
                var isDataSourceGenerator = false;
                var selfAndBaseTypes = attribute.AttributeClass.GetSelfAndBaseTypes();
                
                foreach (var type in selfAndBaseTypes)
                {
                    if (type.IsGenericType && type.TypeArguments.Length > 0)
                    {
                        var originalDef = type.OriginalDefinition;
                        var metadataName = originalDef?.ToDisplayString();
                        
                        if (metadataName?.Contains("DataSourceGeneratorAttribute") == true ||
                            metadataName?.Contains("AsyncDataSourceGeneratorAttribute") == true)
                        {
                            isDataSourceGenerator = true;
                            break;
                        }
                    }
                }
                
                if (isDataSourceGenerator)
                {
                    var typesToValidate = propertySymbol != null 
                        ? ImmutableArray.Create(propertySymbol.Type)
                        : types;
                    CheckDataGenerator(context, attribute, typesToValidate);
                }
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
            // Skip required keyword enforcement if the property is in a class that inherits from System.Attribute
            if (IsInAttributeClass(propertySymbol.ContainingType))
            {
                // Attribute classes don't need to enforce the required keyword for injected properties
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.PropertyRequiredNotSet, propertySymbol.Locations.FirstOrDefault()));
            }
        }

        if (propertySymbol is { IsStatic: true, SetMethod: null })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MustHavePropertySetter, propertySymbol.Locations.FirstOrDefault()));
        }
    }

    private static bool IsInAttributeClass(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        // Check if the type or any of its base types is System.Attribute
        return typeSymbol.IsOrInherits("global::System.Attribute");
    }

    private ImmutableArray<ITypeSymbol> GetTypes(ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? propertySymbol)
    {
        var types = parameters.Select(x => x.Type).Concat(new[] { propertySymbol?.Type }).Where(t => t != null);

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
            ? ImmutableArray.Create(default(TypedConstant))
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

            // Handle params parameters specifically
            if (parameter?.IsParams == true && typeSymbol.IsCollectionType(context.Compilation, out var innerType))
            {
                // For params parameters, validate remaining arguments against the element type
                var remainingArguments = arguments.Skip(i);
                if (remainingArguments.All(x => x.IsNull || CanConvert(context, x, innerType)))
                {
                    break;
                }
            }
            // Handle regular collection types (non-params)
            else if (typeSymbol.IsCollectionType(context.Compilation, out innerType)
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
                        "<null>",
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
        ImmutableArray<ITypeSymbol> testParameterTypes,
        IPropertySymbol? propertySymbol = null)
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
                    : Array.Empty<ITypeSymbol>();

            var methodSymbols = (type as INamedTypeSymbol)?.GetSelfAndBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .ToArray() ?? Array.Empty<IMethodSymbol>();

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

            // Special check for property injection - the method should return exactly the property type or Func<PropertyType>
            if (propertySymbol != null)
            {
                var returnType = dataSourceMethod.ReturnType;
                var propertyType = propertySymbol.Type;
                
                // For property injection, if the return type exactly matches the property type, it's valid
                if (returnType.ToDisplayString() == propertyType.ToDisplayString())
                {
                    return; // Valid property injection
                }
                
                // Check if return type is Func<T> where T matches the property type
                if (returnType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } funcType &&
                    funcType.ToDisplayString().StartsWith("System.Func<"))
                {
                    var funcReturnType = funcType.TypeArguments[0];
                    if (funcReturnType.ToDisplayString() == propertyType.ToDisplayString() ||
                        context.Compilation.HasImplicitConversion(funcReturnType, propertyType))
                    {
                        return; // Valid - Func<T> where T matches property type
                    }
                }
                
                // Check if types are compatible with implicit conversion
                var conversion = context.Compilation.ClassifyConversion(returnType, propertyType);
                if (conversion.IsImplicit || conversion.IsIdentity)
                {
                    return; // Valid property injection with implicit conversion
                }
                
                // For property injection, we don't support IEnumerable - properties need single values
                // If the return type is Func<T>, report T instead since that's what will be injected
                var reportedType = returnType;
                if (returnType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } funcTypeForError &&
                    funcTypeForError.ToDisplayString().StartsWith("System.Func<"))
                {
                    reportedType = funcTypeForError.TypeArguments[0];
                }
                
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        reportedType.ToDisplayString(),
                        propertyType.ToDisplayString())
                );
                return; // Don't continue with further checks - we've already reported the error
            }

            // If we already handled property injection, don't continue
            // This should never happen due to the early return above, but let's be safe
            if (propertySymbol != null)
            {
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

            var dataSourceMethodParameterTypes = dataSourceMethod.Parameters
                .WithoutCancellationTokenParameter()
                .Select(x => x.Type)
                .ToArray();

            // Note: We no longer check if test has multiple parameters when data source doesn't return tuples
            // because the data source method can return arrays of arrays (object[][]) to satisfy multiple parameters

            // Skip type checking if unwrappedTypes is empty (indicating object[] which can contain any types)
            if (unwrappedTypes.Length == 0)
            {
                // object[] can contain any types - skip compile-time type checking
                return;
            }
            var conversions = unwrappedTypes.ZipAll(testParameterTypes,
                (argument, parameter) => 
                {
                    // Handle exact type matches for property injection where types might have different metadata
                    if (propertySymbol != null && 
                        argument != null && 
                        parameter != null && 
                        argument.ToDisplayString() == parameter.ToDisplayString())
                    {
                        return true;
                    }
                    return context.Compilation.HasImplicitConversionOrGenericParameter(argument, parameter);
                });

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
                // Check if any test method parameters are tuple types when data source returns tuples
                // This causes a runtime mismatch: data source provides separate arguments, but method expects tuple parameter
                var tupleParameters = testParameterTypes.Where(p => p is INamedTypeSymbol { IsTupleType: true }).ToArray();
                if (tupleParameters.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.WrongArgumentTypeTestData,
                        attribute.GetLocation(),
                        string.Join(", ", unwrappedTypes),
                        string.Join(", ", testParameterTypes))
                    );
                    return;
                }

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
            return ImmutableArray.Create(type);
        }

        if (context.Symbol is IPropertySymbol)
        {
            // For property injection, we expect a single value, not a collection
            // Only unwrap Func<T> if present, NOT IEnumerable
            if (type is INamedTypeSymbol { IsGenericType: true } propertyFuncType
                && SymbolEqualityComparer.Default.Equals(
                    context.Compilation.GetTypeByMetadataName("System.Func`1"),
                    propertyFuncType.OriginalDefinition))
            {
                isFunc = true;
                type = propertyFuncType.TypeArguments[0];
            }

            return ImmutableArray.Create(type);
        }

        if (methodContainingTestData.ReturnType is not INamedTypeSymbol and not IArrayTypeSymbol)
        {
            return ImmutableArray.Create(methodContainingTestData.ReturnType);
        }

        // Check for Task<T> or ValueTask<T> wrappers first
        if (methodContainingTestData.ReturnType is INamedTypeSymbol { IsGenericType: true } taskType)
        {
            var taskTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            if ((taskTypeSymbol != null && SymbolEqualityComparer.Default.Equals(taskType.OriginalDefinition, taskTypeSymbol)) ||
                (valueTaskTypeSymbol != null && SymbolEqualityComparer.Default.Equals(taskType.OriginalDefinition, valueTaskTypeSymbol)))
            {
                type = taskType.TypeArguments[0];
            }
        }

        if (type.IsIEnumerable(context.Compilation, out var enumerableInnerType))
        {
            type = enumerableInnerType;
        }

        // Check for IAsyncEnumerable<T>
        if (SymbolEqualityComparer.Default.Equals(type, methodContainingTestData.ReturnType) && IsIAsyncEnumerable(type, context.Compilation, out var asyncEnumerableInnerType))
        {
            type = asyncEnumerableInnerType;
        }

        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return ImmutableArray.Create(type);
        }

        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } genericType
            && genericType.ToDisplayString().StartsWith("System.Func<"))
        {
            isFunc = true;
            type = genericType.TypeArguments[0];
        }

        // Check for tuple types first before doing conversion checks
        if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            isTuples = true;
            return ImmutableArray.CreateRange(tupleType.TupleElements.Select(x => x.Type));
        }

        if (testParameterTypes.Length == 1
            && context.Compilation.HasImplicitConversionOrGenericParameter(type, testParameterTypes[0]))
        {
            return ImmutableArray.Create(type);
        }

        // Handle array cases - when a data source returns IEnumerable<T[]> or IAsyncEnumerable<T[]>,
        // each array contains the arguments for one test invocation
        if (type is IArrayTypeSymbol arrayType)
        {
            // Arrays from data sources are always meant to contain test arguments,
            // not to be passed as a single array parameter.
            // Skip compile-time type checking for all array types.
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        return ImmutableArray.Create(type);
    }

    private void CheckDataGenerator(SymbolAnalysisContext context,
        AttributeData attribute,
        ImmutableArray<ITypeSymbol> testDataTypes)
    {
        var selfAndBaseTypes = attribute.AttributeClass?.GetSelfAndBaseTypes() ?? new List<INamedTypeSymbol>
        {
        }.AsReadOnly();

        var baseGeneratorAttribute = selfAndBaseTypes
            .FirstOrDefault(x => x.AllInterfaces.Any(i => i.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IDataSourceAttribute.WithGlobalPrefix));

        // If interface check fails, use name-based detection as fallback
        if (baseGeneratorAttribute is null)
        {
            // Check if this is a known data source generator by name
            var isDataSourceGenerator = selfAndBaseTypes.Any(type =>
            {
                var typeName = type.Name;
                if (type.IsGenericType)
                {
                    var genericTypeName = type.OriginalDefinition?.Name ?? typeName;
                    return genericTypeName.StartsWith("DataSourceGeneratorAttribute") ||
                           genericTypeName.StartsWith("AsyncDataSourceGeneratorAttribute") ||
                           genericTypeName.StartsWith("ClassDataSourceAttribute");
                }
                return typeName == "ArgumentsAttribute";
            });
            
            if (!isDataSourceGenerator)
            {
                return;
            }
        }

        if (testDataTypes.Any(x => x.IsGenericDefinition()))
        {
            return;
        }

        // Get type arguments from the attribute or its base types
        var typeArguments = ImmutableArray<ITypeSymbol>.Empty;
        
        // First, try the same approach as the source generator: look for ITypedDataSourceAttribute<T> interface
        var typedInterface = attribute.AttributeClass?.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType && 
                i.ConstructedFrom.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");
                
        if (typedInterface != null)
        {
            // If the type is a tuple, extract its elements
            if (typedInterface.TypeArguments is
                [
                    INamedTypeSymbol { IsTupleType: true } tupleType
                ])
            {
                typeArguments = ImmutableArray.CreateRange(tupleType.TupleElements.Select(x => x.Type));
            }
            else
            {
                typeArguments = typedInterface.TypeArguments;
            }
        }
        else
        {
            // Fallback: Look specifically for DataSourceGeneratorAttribute or AsyncDataSourceGeneratorAttribute base types
            // which contain the actual data type arguments, not the custom attribute's type parameters
            foreach (var baseType in selfAndBaseTypes)
            {
                if (baseType.IsGenericType && !baseType.TypeArguments.IsEmpty)
                {
                    var originalDef = baseType.OriginalDefinition;
                    var metadataName = originalDef?.ToDisplayString();
                    
                    if (metadataName?.Contains("DataSourceGeneratorAttribute") == true ||
                        metadataName?.Contains("AsyncDataSourceGeneratorAttribute") == true)
                    {
                        typeArguments = baseType.TypeArguments;
                        break;
                    }
                }
            }
            
            // Final fallback: if no specific data source generator base type found, use the attribute's own type arguments
            if (typeArguments.IsEmpty && attribute.AttributeClass?.TypeArguments.IsEmpty == false)
            {
                typeArguments = attribute.AttributeClass.TypeArguments;
            }
        }

        // If still no type arguments (like ArgumentsAttribute which returns object?[]?),
        // skip compile-time type checking as it will be validated at runtime
        if (typeArguments.IsEmpty)
        {
            return;
        }

        // Check if there's a mismatch in the number of types
        if (typeArguments.Length != testDataTypes.Length)
        {
            // If testDataTypes is empty, show "<none>" instead of empty string
            var testTypesDisplay = testDataTypes.Length == 0 ? "" : string.Join(", ", testDataTypes);
            context.ReportDiagnostic(Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                    attribute.GetLocation() ?? context.Symbol.Locations.FirstOrDefault(),
                    string.Join(", ", typeArguments), testTypesDisplay));
            return;
        }

        var conversions = typeArguments.ZipAll(testDataTypes,
            (returnType, parameterType) => context.Compilation.HasImplicitConversionOrGenericParameter(returnType, parameterType));

        if (conversions.All(x => x))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                attribute.GetLocation() ?? context.Symbol.Locations.FirstOrDefault(),
                string.Join(", ", typeArguments), string.Join(", ", testDataTypes)));
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

    private static bool IsIAsyncEnumerable(ITypeSymbol type, Compilation compilation, [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        innerType = null;

        // Get IAsyncEnumerable<T> type
        var asyncEnumerableType = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        if (asyncEnumerableType == null)
        {
            return false;
        }

        // Check if the type itself is IAsyncEnumerable<T>
        if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition.Equals(asyncEnumerableType, SymbolEqualityComparer.Default))
        {
            innerType = namedType.TypeArguments[0];
            return true;
        }

        // Check interfaces
        var asyncEnumerableInterface = type.AllInterfaces
            .FirstOrDefault(i => i.OriginalDefinition.Equals(asyncEnumerableType, SymbolEqualityComparer.Default));

        if (asyncEnumerableInterface != null)
        {
            innerType = asyncEnumerableInterface.TypeArguments[0];
            return true;
        }

        return false;
    }
}
