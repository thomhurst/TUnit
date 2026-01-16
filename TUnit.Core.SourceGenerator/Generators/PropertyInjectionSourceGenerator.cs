using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Extracted;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// V2 PropertyInjectionSourceGenerator using primitive-only models for proper incremental caching.
/// All symbol access happens in the transform step; only primitives are stored in the model.
/// </summary>
[Generator]
public sealed class PropertyInjectionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        // Pipeline 1: Find properties with IDataSourceAttribute and group by containing class
        var propertyDataSources = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is PropertyDeclarationSyntax,
                transform: static (ctx, _) => ExtractPropertyDataSource(ctx))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!);

        // Collect and group by class
        var groupedByClass = propertyDataSources
            .Collect()
            .SelectMany(static (properties, _) => GroupPropertiesByClass(properties));

        var classesWithEnabled = groupedByClass.Combine(enabledProvider);

        context.RegisterSourceOutput(classesWithEnabled, static (ctx, data) =>
        {
            var (classModel, isEnabled) = data;
            if (!isEnabled || classModel.Properties.AsArray().Length == 0)
                return;
            GeneratePropertyInjectionSource(ctx, classModel);
        });

        // Pipeline 2: Find IAsyncInitializer types with properties returning IAsyncInitializer
        var asyncInitializers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => ExtractAsyncInitializerModel(ctx))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!);

        var asyncInitializersWithEnabled = asyncInitializers
            .Collect()
            .SelectMany(static (models, _) => models.Distinct())
            .Combine(enabledProvider);

        context.RegisterSourceOutput(asyncInitializersWithEnabled, static (ctx, data) =>
        {
            var (model, isEnabled) = data;
            if (!isEnabled || model.Properties.AsArray().Length == 0)
                return;
            GenerateInitializerPropertySource(ctx, model);
        });

        // Pipeline 3: Discover concrete generic types from inheritance chains and IDataSourceAttribute type arguments
        var concreteGenericTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax or PropertyDeclarationSyntax,
                transform: static (ctx, _) => ExtractConcreteGenericTypes(ctx))
            .Where(static x => x.Length > 0)
            .SelectMany(static (types, _) => types);

        // Collect and deduplicate by fully qualified type name
        var distinctConcreteGenerics = concreteGenericTypes
            .Collect()
            .SelectMany(static (types, _) => DeduplicateConcreteGenericTypes(types));

        var concreteGenericsWithEnabled = distinctConcreteGenerics.Combine(enabledProvider);

        // Pipeline 4 & 5: Generate source for concrete generic types
        context.RegisterSourceOutput(concreteGenericsWithEnabled, static (ctx, data) =>
        {
            var (model, isEnabled) = data;
            if (!isEnabled)
                return;

            // Generate property data source if the type has data source properties
            if (model.HasDataSourceProperties && model.DataSourceProperties.AsArray().Length > 0)
            {
                GenerateGenericPropertyInjectionSource(ctx, model);
            }

            // Generate initializer property source if the type has initializer properties
            if (model.InitializerProperties.AsArray().Length > 0)
            {
                GenerateGenericInitializerPropertySource(ctx, model);
            }
        });
    }

    #region Property Data Source Extraction

    /// <summary>
    /// Intermediate model for a single property with data source (before grouping by class).
    /// </summary>
    private sealed record PropertyWithClass(
        string ClassFullyQualifiedName,
        string SafeClassName,
        PropertyDataSourceModel Property);

    private static PropertyWithClass? ExtractPropertyDataSource(GeneratorSyntaxContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax propertyDecl)
            return null;

        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(propertyDecl) is not IPropertySymbol propertySymbol)
            return null;

        // Must have a setter
        if (propertySymbol.SetMethod == null)
            return null;

        var containingType = propertySymbol.ContainingType;
        if (containingType == null)
            return null;

        // Skip types that are not publicly accessible
        if (!IsPubliclyAccessible(containingType))
            return null;

        // Skip open generic types
        if (containingType.IsUnboundGenericType || containingType.TypeParameters.Length > 0)
            return null;

        // Find IDataSourceAttribute
        var dataSourceInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");
        if (dataSourceInterface == null)
            return null;

        AttributeData? dataSourceAttribute = null;
        foreach (var attr in propertySymbol.GetAttributes())
        {
            if (attr.AttributeClass != null &&
                attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default))
            {
                dataSourceAttribute = attr;
                break;
            }
        }

        if (dataSourceAttribute?.AttributeClass == null)
            return null;

        // Extract all data as primitives
        var propertyModel = ExtractPropertyModel(propertySymbol, dataSourceAttribute);

        return new PropertyWithClass(
            ClassFullyQualifiedName: containingType.GloballyQualified(),
            SafeClassName: GetSafeClassName(containingType),
            Property: propertyModel);
    }

    private static PropertyDataSourceModel ExtractPropertyModel(IPropertySymbol property, AttributeData attribute, INamedTypeSymbol? containingTypeOverride = null)
    {
        var propertyType = property.Type;
        var isNullableValueType = propertyType is INamedTypeSymbol
        {
            IsGenericType: true,
            ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        };

        // Check if the original property type is a type parameter (e.g., T Provider { get; })
        // We need to use the type parameter name in UnsafeAccessor for generic types
        string? propertyTypeAsTypeParameter = null;
        if (property.OriginalDefinition.Type is ITypeParameterSymbol typeParam)
        {
            propertyTypeAsTypeParameter = typeParam.Name;
        }

        // Format constructor arguments
        var ctorArgs = attribute.ConstructorArguments
            .Select(FormatTypedConstant)
            .ToArray();

        // Format named arguments
        var namedArgs = attribute.NamedArguments
            .Select(na => new NamedArgModel
            {
                Name = na.Key,
                FormattedValue = FormatTypedConstant(na.Value)
            })
            .ToArray();

        // Use the override if provided (for closed generic types), otherwise use the declaring type
        var containingType = containingTypeOverride ?? property.ContainingType;

        // For generic types, extract the open generic type definition and type parameters
        string? openGenericType = null;
        string? typeParameters = null;
        string? typeArguments = null;
        string? typeConstraints = null;

        if (containingType.IsGenericType)
        {
            var originalDefinition = containingType.OriginalDefinition;
            openGenericType = originalDefinition.ToDisplayString();
            typeParameters = string.Join(", ", originalDefinition.TypeParameters.Select(tp => tp.Name));
            typeArguments = string.Join(", ", containingType.TypeArguments.Select(ta => ta.ToDisplayString()));
            typeConstraints = GetTypeParameterConstraints(originalDefinition.TypeParameters);
        }

        return new PropertyDataSourceModel
        {
            PropertyName = property.Name,
            PropertyTypeFullyQualified = GetNonNullableTypeName(propertyType),
            PropertyTypeForTypeof = GetNonNullableTypeString(propertyType),
            ContainingTypeFullyQualified = containingType.ToDisplayString(),
            ContainingTypeClrName = GetClrTypeName(containingType),
            ContainingTypeOpenGeneric = openGenericType,
            GenericTypeParameters = typeParameters,
            GenericTypeArguments = typeArguments,
            GenericTypeConstraints = typeConstraints,
            IsInitOnly = property.SetMethod?.IsInitOnly == true,
            IsContainingTypeGeneric = containingType.IsGenericType,
            IsStatic = property.IsStatic,
            PropertyTypeAsTypeParameter = propertyTypeAsTypeParameter,
            IsValueType = propertyType.IsValueType,
            IsNullableValueType = isNullableValueType,
            AttributeTypeName = attribute.AttributeClass!.ToDisplayString(),
            ConstructorArgs = new EquatableArray<string>(ctorArgs),
            NamedArgs = new EquatableArray<NamedArgModel>(namedArgs)
        };
    }

    private static IEnumerable<ClassPropertyInjectionModel> GroupPropertiesByClass(
        ImmutableArray<PropertyWithClass> properties)
    {
        return properties
            .GroupBy(p => p.ClassFullyQualifiedName)
            .Select(g => new ClassPropertyInjectionModel
            {
                ClassFullyQualifiedName = g.Key,
                SafeClassName = g.First().SafeClassName,
                Properties = new EquatableArray<PropertyDataSourceModel>(
                    g.Select(p => p.Property).Distinct().ToArray())
            });
    }

    #endregion

    #region IAsyncInitializer Extraction

    private static AsyncInitializerModel? ExtractAsyncInitializerModel(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDecl)
            return null;

        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol)
            return null;

        // Skip non-public/internal types
        if (!IsPubliclyAccessible(typeSymbol))
            return null;

        // Skip open generic types
        if (typeSymbol.IsUnboundGenericType || typeSymbol.TypeParameters.Length > 0)
            return null;

        var asyncInitializerInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.Interfaces.IAsyncInitializer");
        if (asyncInitializerInterface == null)
            return null;

        // Check if this type implements IAsyncInitializer
        if (!typeSymbol.AllInterfaces.Contains(asyncInitializerInterface, SymbolEqualityComparer.Default))
            return null;

        // Find properties that return IAsyncInitializer types
        var initializerProperties = new List<InitializerPropertyModel>();

        var allProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && !p.IsStatic && !p.IsIndexer);

        foreach (var property in allProperties)
        {
            if (property.Type is INamedTypeSymbol propertyType)
            {
                if (propertyType.AllInterfaces.Contains(asyncInitializerInterface, SymbolEqualityComparer.Default) ||
                    SymbolEqualityComparer.Default.Equals(propertyType, asyncInitializerInterface))
                {
                    initializerProperties.Add(new InitializerPropertyModel
                    {
                        PropertyName = property.Name,
                        PropertyTypeFullyQualified = property.Type.GloballyQualified()
                    });
                }
            }
        }

        if (initializerProperties.Count == 0)
            return null;

        return new AsyncInitializerModel
        {
            TypeFullyQualified = typeSymbol.GloballyQualified(),
            SafeTypeName = GetSafeClassName(typeSymbol),
            Properties = new EquatableArray<InitializerPropertyModel>(initializerProperties.ToArray())
        };
    }

    #endregion

    #region Concrete Generic Type Discovery

    /// <summary>
    /// Extracts concrete generic types from the current syntax node (type declarations and properties).
    /// Looks for concrete generic types in:
    /// 1. Inheritance chains (base types)
    /// 2. IDataSourceAttribute type arguments
    /// </summary>
    private static ImmutableArray<ConcreteGenericTypeModel> ExtractConcreteGenericTypes(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;
        var results = new List<ConcreteGenericTypeModel>();

        var dataSourceInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");
        var asyncInitializerInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.Interfaces.IAsyncInitializer");

        if (dataSourceInterface == null || asyncInitializerInterface == null)
            return ImmutableArray<ConcreteGenericTypeModel>.Empty;

        // Discovery from type declarations (inheritance chains)
        if (context.Node is TypeDeclarationSyntax typeDecl)
        {
            if (semanticModel.GetDeclaredSymbol(typeDecl) is INamedTypeSymbol typeSymbol)
            {
                // Walk inheritance chain to find concrete generic base types
                var baseType = typeSymbol.BaseType;
                while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                {
                    if (IsConcreteGenericType(baseType))
                    {
                        var model = CreateConcreteGenericModel(baseType, dataSourceInterface, asyncInitializerInterface);
                        if (model != null)
                        {
                            results.Add(model);
                        }
                    }
                    baseType = baseType.BaseType;
                }

                // Check implemented interfaces for concrete generics
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (IsConcreteGenericType(iface))
                    {
                        var model = CreateConcreteGenericModel(iface, dataSourceInterface, asyncInitializerInterface);
                        if (model != null)
                        {
                            results.Add(model);
                        }
                    }
                }
            }
        }

        // Discovery from property declarations (IDataSourceAttribute type arguments)
        if (context.Node is PropertyDeclarationSyntax propertyDecl)
        {
            if (semanticModel.GetDeclaredSymbol(propertyDecl) is IPropertySymbol propertySymbol)
            {
                foreach (var attr in propertySymbol.GetAttributes())
                {
                    if (attr.AttributeClass == null)
                        continue;

                    // Check if attribute implements IDataSourceAttribute
                    if (!attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default))
                        continue;

                    // Check attribute type arguments for concrete generic types
                    DiscoverGenericTypesFromTypeArguments(attr.AttributeClass, dataSourceInterface, asyncInitializerInterface, results);

                    // Check constructor arguments for type parameters
                    foreach (var ctorArg in attr.ConstructorArguments)
                    {
                        // Handle arrays - iterate over values to find concrete generic types
                        if (ctorArg.Kind == TypedConstantKind.Array)
                        {
                            foreach (var arrayElement in ctorArg.Values)
                            {
                                if (arrayElement.Value is INamedTypeSymbol elementType && IsConcreteGenericType(elementType))
                                {
                                    var model = CreateConcreteGenericModel(elementType, dataSourceInterface, asyncInitializerInterface);
                                    if (model != null)
                                    {
                                        results.Add(model);
                                    }
                                }
                            }
                            continue;
                        }

                        if (ctorArg.Value is INamedTypeSymbol argType && IsConcreteGenericType(argType))
                        {
                            var model = CreateConcreteGenericModel(argType, dataSourceInterface, asyncInitializerInterface);
                            if (model != null)
                            {
                                results.Add(model);
                            }
                        }
                    }
                }

                // Also check if the property type itself is a concrete generic
                if (propertySymbol.Type is INamedTypeSymbol propertyType && IsConcreteGenericType(propertyType))
                {
                    var model = CreateConcreteGenericModel(propertyType, dataSourceInterface, asyncInitializerInterface);
                    if (model != null)
                    {
                        results.Add(model);
                    }
                }
            }
        }

        return results.Count > 0 ? results.ToImmutableArray() : ImmutableArray<ConcreteGenericTypeModel>.Empty;
    }

    /// <summary>
    /// Recursively discovers concrete generic types from type arguments.
    /// </summary>
    private static void DiscoverGenericTypesFromTypeArguments(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol dataSourceInterface,
        INamedTypeSymbol asyncInitializerInterface,
        List<ConcreteGenericTypeModel> results)
    {
        if (!typeSymbol.IsGenericType)
            return;

        foreach (var typeArg in typeSymbol.TypeArguments)
        {
            if (typeArg is INamedTypeSymbol namedTypeArg)
            {
                if (IsConcreteGenericType(namedTypeArg))
                {
                    var model = CreateConcreteGenericModel(namedTypeArg, dataSourceInterface, asyncInitializerInterface);
                    if (model != null)
                    {
                        results.Add(model);
                    }
                }

                // Recurse into nested type arguments
                if (namedTypeArg.IsGenericType)
                {
                    DiscoverGenericTypesFromTypeArguments(namedTypeArg, dataSourceInterface, asyncInitializerInterface, results);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a type is a concrete instantiation of a generic type (not an open generic).
    /// </summary>
    private static bool IsConcreteGenericType(INamedTypeSymbol type)
    {
        return type.IsGenericType && !type.IsUnboundGenericType && type.TypeArguments.All(t => t.TypeKind != TypeKind.TypeParameter);
    }

    /// <summary>
    /// Creates a ConcreteGenericTypeModel for a discovered concrete generic type.
    /// Returns null if the type doesn't have any relevant properties or interfaces.
    /// </summary>
    private static ConcreteGenericTypeModel? CreateConcreteGenericModel(
        INamedTypeSymbol concreteType,
        INamedTypeSymbol dataSourceInterface,
        INamedTypeSymbol asyncInitializerInterface)
    {
        // Check if this type implements IAsyncInitializer
        var implementsIAsyncInitializer = concreteType.AllInterfaces.Contains(asyncInitializerInterface, SymbolEqualityComparer.Default);

        // Find data source properties (walk inheritance chain)
        var dataSourceProperties = new List<PropertyDataSourceModel>();
        var initializerProperties = new List<InitializerPropertyModel>();

        var currentType = concreteType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is not IPropertySymbol property)
                    continue;

                // Check for IDataSourceAttribute on property
                if (property.SetMethod != null)
                {
                    foreach (var attr in property.GetAttributes())
                    {
                        if (attr.AttributeClass != null &&
                            attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default))
                        {
                            // Pass currentType as the containing type override for closed generic types
                            dataSourceProperties.Add(ExtractPropertyModel(property, attr, currentType));
                            break;
                        }
                    }
                }

                // Check if property returns IAsyncInitializer (only if the type itself implements IAsyncInitializer)
                if (implementsIAsyncInitializer && property.GetMethod != null && !property.IsStatic && !property.IsIndexer)
                {
                    if (property.Type is INamedTypeSymbol propertyType)
                    {
                        if (propertyType.AllInterfaces.Contains(asyncInitializerInterface, SymbolEqualityComparer.Default) ||
                            SymbolEqualityComparer.Default.Equals(propertyType, asyncInitializerInterface))
                        {
                            initializerProperties.Add(new InitializerPropertyModel
                            {
                                PropertyName = property.Name,
                                PropertyTypeFullyQualified = property.Type.GloballyQualified()
                            });
                        }
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        // Only return a model if the type has something relevant
        var hasDataSourceProperties = dataSourceProperties.Count > 0;
        var hasInitializerProperties = initializerProperties.Count > 0;

        if (!hasDataSourceProperties && !hasInitializerProperties)
            return null;

        return new ConcreteGenericTypeModel
        {
            ConcreteTypeFullyQualified = concreteType.GloballyQualified(),
            SafeTypeName = GetSafeClassName(concreteType),
            ImplementsIAsyncInitializer = implementsIAsyncInitializer,
            HasDataSourceProperties = hasDataSourceProperties,
            DataSourceProperties = new EquatableArray<PropertyDataSourceModel>(dataSourceProperties.ToArray()),
            InitializerProperties = new EquatableArray<InitializerPropertyModel>(initializerProperties.ToArray())
        };
    }

    /// <summary>
    /// Deduplicates concrete generic type models by their fully qualified name.
    /// </summary>
    private static IEnumerable<ConcreteGenericTypeModel> DeduplicateConcreteGenericTypes(
        ImmutableArray<ConcreteGenericTypeModel> types)
    {
        var seen = new HashSet<string>();
        foreach (var type in types)
        {
            if (seen.Add(type.ConcreteTypeFullyQualified))
            {
                yield return type;
            }
        }
    }

    #endregion

    #region Code Generation

    private static void GeneratePropertyInjectionSource(SourceProductionContext context, ClassPropertyInjectionModel model)
    {
        var sourceClassName = $"PropertyInjectionSource_{Math.Abs(model.ClassFullyQualifiedName.GetHashCode()):x}";
        var fileName = $"{model.SafeClassName}_PropertyInjection.g.cs";

        var sb = new StringBuilder();
        WriteFileHeader(sb);

        // Module initializer
        sb.AppendLine($"internal static class {model.SafeClassName}_PropertyInjectionInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine($"        global::TUnit.Core.PropertySourceRegistry.Register(typeof({model.ClassFullyQualifiedName}), new {sourceClassName}());");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        // Property source class
        sb.AppendLine($"internal sealed class {sourceClassName} : IPropertySource");
        sb.AppendLine("{");
        sb.AppendLine($"    public Type Type => typeof({model.ClassFullyQualifiedName});");
        sb.AppendLine("    public bool ShouldInitialize => true;");
        sb.AppendLine();

        // Generate UnsafeAccessor methods for init-only properties on non-generic types
        foreach (var prop in model.Properties)
        {
            if (prop.IsInitOnly && !prop.IsContainingTypeGeneric)
            {
                var backingFieldName = $"<{prop.PropertyName}>k__BackingField";

                // For non-generic types: use regular UnsafeAccessor on .NET 8+
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    private static extern ref {prop.PropertyTypeFullyQualified} Get{prop.PropertyName}BackingField({prop.ContainingTypeFullyQualified} instance);");
                sb.AppendLine("#endif");
                sb.AppendLine();
            }
        }

        // GetPropertyMetadata method
        sb.AppendLine("    public IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata()");
        sb.AppendLine("    {");

        foreach (var prop in model.Properties)
        {
            GeneratePropertyMetadata(sb, prop, model.ClassFullyQualifiedName, model.SafeClassName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        // Generate generic accessor classes for init-only properties on generic types
        // These must be outside the property source class and be generic themselves
        foreach (var prop in model.Properties)
        {
            if (prop.IsInitOnly && prop.IsContainingTypeGeneric && prop.GenericTypeParameters != null && prop.ContainingTypeOpenGeneric != null)
            {
                var backingFieldName = $"<{prop.PropertyName}>k__BackingField";
                var accessorClassName = $"{model.SafeClassName}_{prop.PropertyName}_GenericAccessor";
                var constraintsClause = prop.GenericTypeConstraints != null ? $" {prop.GenericTypeConstraints}" : "";
                // Use type parameter name if property type is a type parameter (e.g., T), otherwise use concrete type
                var returnType = prop.PropertyTypeAsTypeParameter ?? prop.PropertyTypeFullyQualified;

                sb.AppendLine();
                sb.AppendLine("#if NET9_0_OR_GREATER");
                sb.AppendLine($"internal static class {accessorClassName}<{prop.GenericTypeParameters}>{constraintsClause}");
                sb.AppendLine("{");
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    public static extern ref {returnType} GetBackingField({prop.ContainingTypeOpenGeneric} instance);");
                sb.AppendLine("}");
                sb.AppendLine("#endif");
            }
        }

        context.AddSource(fileName, sb.ToString());
    }

    private static void GeneratePropertyMetadata(StringBuilder sb, PropertyDataSourceModel prop, string classTypeName, string safeClassName)
    {
        var ctorArgsStr = string.Join(", ", prop.ConstructorArgs);

        sb.AppendLine("        yield return new PropertyInjectionMetadata");
        sb.AppendLine("        {");
        sb.AppendLine($"            PropertyName = \"{prop.PropertyName}\",");
        sb.AppendLine($"            PropertyType = typeof({prop.PropertyTypeForTypeof}),");
        sb.AppendLine($"            ContainingType = typeof({prop.ContainingTypeFullyQualified}),");

        // CreateDataSource delegate
        sb.AppendLine("            CreateDataSource = () =>");
        sb.AppendLine("            {");
        sb.AppendLine($"                var dataSource = new {prop.AttributeTypeName}({ctorArgsStr});");

        foreach (var namedArg in prop.NamedArgs)
        {
            sb.AppendLine($"                dataSource.{namedArg.Name} = {namedArg.FormattedValue};");
        }

        sb.AppendLine("                return dataSource;");
        sb.AppendLine("            },");

        // SetProperty delegate
        sb.AppendLine("            SetProperty = (instance, value) =>");
        sb.AppendLine("            {");
        sb.AppendLine($"                var typedInstance = ({classTypeName})instance;");

        var castExpression = GetPropertyCastExpression(prop);

        if (prop.IsInitOnly)
        {
            if (prop.IsContainingTypeGeneric && prop.GenericTypeArguments != null)
            {
                // For generic types: .NET 9+ uses generic accessor class, older versions use reflection
                var accessorClassName = $"{safeClassName}_{prop.PropertyName}_GenericAccessor";

                sb.AppendLine("#if NET9_0_OR_GREATER");
                sb.AppendLine($"                {accessorClassName}<{prop.GenericTypeArguments}>.GetBackingField(typedInstance) = {castExpression};");
                sb.AppendLine("#else");
                sb.AppendLine($"                var backingField = typeof({prop.ContainingTypeFullyQualified}).GetField(\"<{prop.PropertyName}>k__BackingField\",");
                sb.AppendLine("                    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
                sb.AppendLine("                backingField.SetValue(typedInstance, value);");
                sb.AppendLine("#endif");
            }
            else
            {
                // For non-generic types: .NET 8+ uses UnsafeAccessor
                sb.AppendLine("#if NET8_0_OR_GREATER");
                if (prop.ContainingTypeFullyQualified != classTypeName)
                {
                    sb.AppendLine($"                Get{prop.PropertyName}BackingField(({prop.ContainingTypeFullyQualified})typedInstance) = {castExpression};");
                }
                else
                {
                    sb.AppendLine($"                Get{prop.PropertyName}BackingField(typedInstance) = {castExpression};");
                }
                sb.AppendLine("#else");
                sb.AppendLine($"                var backingField = typeof({prop.ContainingTypeFullyQualified}).GetField(\"<{prop.PropertyName}>k__BackingField\",");
                sb.AppendLine("                    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
                sb.AppendLine("                backingField.SetValue(typedInstance, value);");
                sb.AppendLine("#endif");
            }
        }
        else if (prop.IsStatic)
        {
            sb.AppendLine($"                {prop.ContainingTypeFullyQualified}.{prop.PropertyName} = {castExpression};");
        }
        else
        {
            sb.AppendLine($"                typedInstance.{prop.PropertyName} = {castExpression};");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private static void GenerateInitializerPropertySource(SourceProductionContext context, AsyncInitializerModel model)
    {
        var fileName = $"{model.SafeTypeName}_InitializerProperties.g.cs";

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using TUnit.Core.Discovery;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Generated;");
        sb.AppendLine();

        sb.AppendLine($"internal static class {model.SafeTypeName}_InitializerPropertiesInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine($"        InitializerPropertyRegistry.Register(typeof({model.TypeFullyQualified}), new InitializerPropertyInfo[]");
        sb.AppendLine("        {");

        foreach (var prop in model.Properties)
        {
            sb.AppendLine("            new InitializerPropertyInfo");
            sb.AppendLine("            {");
            sb.AppendLine($"                PropertyName = \"{prop.PropertyName}\",");
            sb.AppendLine($"                PropertyType = typeof({prop.PropertyTypeFullyQualified}),");
            sb.AppendLine($"                GetValue = static obj => (({model.TypeFullyQualified})obj).{prop.PropertyName}");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(fileName, sb.ToString());
    }

    /// <summary>
    /// Generates property injection source for a concrete generic type.
    /// Similar to GeneratePropertyInjectionSource but uses ConcreteGenericTypeModel.
    /// </summary>
    private static void GenerateGenericPropertyInjectionSource(SourceProductionContext context, ConcreteGenericTypeModel model)
    {
        var sourceClassName = $"PropertyInjectionSource_Generic_{Math.Abs(model.ConcreteTypeFullyQualified.GetHashCode()):x}";
        var fileName = $"{model.SafeTypeName}_Generic_PropertyInjection.g.cs";

        var sb = new StringBuilder();
        WriteFileHeader(sb);

        // Module initializer
        sb.AppendLine($"internal static class {model.SafeTypeName}_Generic_PropertyInjectionInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine($"        global::TUnit.Core.PropertySourceRegistry.Register(typeof({model.ConcreteTypeFullyQualified}), new {sourceClassName}());");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        // Property source class
        sb.AppendLine($"internal sealed class {sourceClassName} : IPropertySource");
        sb.AppendLine("{");
        sb.AppendLine($"    public Type Type => typeof({model.ConcreteTypeFullyQualified});");
        sb.AppendLine("    public bool ShouldInitialize => true;");
        sb.AppendLine();

        // Generate UnsafeAccessor methods for init-only properties on non-generic types
        foreach (var prop in model.DataSourceProperties)
        {
            if (prop.IsInitOnly && !prop.IsContainingTypeGeneric)
            {
                var backingFieldName = $"<{prop.PropertyName}>k__BackingField";

                // For non-generic types: use regular UnsafeAccessor on .NET 8+
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    private static extern ref {prop.PropertyTypeFullyQualified} Get{prop.PropertyName}BackingField({prop.ContainingTypeFullyQualified} instance);");
                sb.AppendLine("#endif");
                sb.AppendLine();
            }
        }

        // GetPropertyMetadata method
        sb.AppendLine("    public IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata()");
        sb.AppendLine("    {");

        foreach (var prop in model.DataSourceProperties)
        {
            GeneratePropertyMetadata(sb, prop, model.ConcreteTypeFullyQualified, model.SafeTypeName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        // Generate generic accessor classes for init-only properties on generic types
        foreach (var prop in model.DataSourceProperties)
        {
            if (prop.IsInitOnly && prop.IsContainingTypeGeneric && prop.GenericTypeParameters != null && prop.ContainingTypeOpenGeneric != null)
            {
                var backingFieldName = $"<{prop.PropertyName}>k__BackingField";
                var accessorClassName = $"{model.SafeTypeName}_{prop.PropertyName}_GenericAccessor";
                var constraintsClause = prop.GenericTypeConstraints != null ? $" {prop.GenericTypeConstraints}" : "";
                // Use type parameter name if property type is a type parameter (e.g., T), otherwise use concrete type
                var returnType = prop.PropertyTypeAsTypeParameter ?? prop.PropertyTypeFullyQualified;

                sb.AppendLine();
                sb.AppendLine("#if NET9_0_OR_GREATER");
                sb.AppendLine($"internal static class {accessorClassName}<{prop.GenericTypeParameters}>{constraintsClause}");
                sb.AppendLine("{");
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    public static extern ref {returnType} GetBackingField({prop.ContainingTypeOpenGeneric} instance);");
                sb.AppendLine("}");
                sb.AppendLine("#endif");
            }
        }

        context.AddSource(fileName, sb.ToString());
    }

    /// <summary>
    /// Generates initializer property source for a concrete generic type.
    /// Similar to GenerateInitializerPropertySource but uses ConcreteGenericTypeModel.
    /// </summary>
    private static void GenerateGenericInitializerPropertySource(SourceProductionContext context, ConcreteGenericTypeModel model)
    {
        var fileName = $"{model.SafeTypeName}_Generic_InitializerProperties.g.cs";

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using TUnit.Core.Discovery;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Generated;");
        sb.AppendLine();

        sb.AppendLine($"internal static class {model.SafeTypeName}_Generic_InitializerPropertiesInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine($"        InitializerPropertyRegistry.Register(typeof({model.ConcreteTypeFullyQualified}), new InitializerPropertyInfo[]");
        sb.AppendLine("        {");

        foreach (var prop in model.InitializerProperties)
        {
            sb.AppendLine("            new InitializerPropertyInfo");
            sb.AppendLine("            {");
            sb.AppendLine($"                PropertyName = \"{prop.PropertyName}\",");
            sb.AppendLine($"                PropertyType = typeof({prop.PropertyTypeFullyQualified}),");
            sb.AppendLine($"                GetValue = static obj => (({model.ConcreteTypeFullyQualified})obj).{prop.PropertyName}");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(fileName, sb.ToString());
    }

    #endregion

    #region Helper Methods

    private static void WriteFileHeader(StringBuilder sb)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using TUnit.Core;");
        sb.AppendLine("using TUnit.Core.Interfaces.SourceGenerator;");
        sb.AppendLine("using TUnit.Core.Enums;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Core;");
        sb.AppendLine();
    }

    private static bool IsPubliclyAccessible(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public &&
            typeSymbol.DeclaredAccessibility != Accessibility.Internal)
        {
            return false;
        }

        var containingType = typeSymbol.ContainingType;
        while (containingType != null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public &&
                containingType.DeclaredAccessibility != Accessibility.Internal)
            {
                return false;
            }

            if (containingType.IsUnboundGenericType || containingType.TypeParameters.Length > 0)
            {
                return false;
            }

            containingType = containingType.ContainingType;
        }

        return true;
    }

    private static string GetSafeClassName(INamedTypeSymbol classSymbol)
    {
        var fullyQualified = classSymbol.GloballyQualified();
        return fullyQualified
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("+", "_");
    }

    private static string GetPropertyCastExpression(PropertyDataSourceModel prop)
    {
        if (prop.IsValueType && !prop.IsNullableValueType)
        {
            return $"({prop.PropertyTypeFullyQualified})value!";
        }
        return $"({prop.PropertyTypeFullyQualified})value";
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        return constant.Kind switch
        {
            TypedConstantKind.Primitive when constant.Value is string str => $"\"{EscapeString(str)}\"",
            TypedConstantKind.Primitive when constant.Value is bool b => b ? "true" : "false",
            TypedConstantKind.Primitive when constant.Value is char c => $"'{EscapeChar(c)}'",
            TypedConstantKind.Primitive => constant.Value?.ToString() ?? "null",
            TypedConstantKind.Enum => FormatEnumConstant(constant),
            TypedConstantKind.Type => FormatTypeConstant(constant),
            TypedConstantKind.Array => FormatArrayConstant(constant),
            _ => constant.Value?.ToString() ?? "null"
        };
    }

    private static string EscapeString(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }

    private static string FormatArrayConstant(TypedConstant constant)
    {
        if (constant.Type is IArrayTypeSymbol arrayType)
        {
            var elementTypeName = GetNonNullableTypeString(arrayType.ElementType);
            var elements = string.Join(", ", constant.Values.Select(FormatTypedConstant));
            return $"new {elementTypeName}[] {{ {elements} }}";
        }

        var fallbackElements = string.Join(", ", constant.Values.Select(FormatTypedConstant));
        return $"new object[] {{ {fallbackElements} }}";
    }

    private static string FormatEnumConstant(TypedConstant constant)
    {
        if (constant is { Type: not null, Value: not null })
        {
            var enumTypeName = constant.Type.ToDisplayString();
            return $"({enumTypeName}){constant.Value}";
        }
        return constant.Value?.ToString() ?? "null";
    }

    private static string FormatTypeConstant(TypedConstant constant)
    {
        if (constant.Value is ITypeSymbol typeSymbol)
        {
            var displayString = GetNonNullableTypeString(typeSymbol);
            return $"typeof({displayString})";
        }
        return $"typeof({constant.Value})";
    }

    private static string GetNonNullableTypeString(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            if (typeSymbol is INamedTypeSymbol { IsReferenceType: true })
            {
                return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).GloballyQualified();
            }
        }

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return namedType.TypeArguments[0].GloballyQualified();
        }

        var displayString = typeSymbol.GloballyQualified();

        if (displayString.EndsWith("?"))
        {
            displayString = displayString.TrimEnd('?');
        }

        return displayString;
    }

    private static string GetNonNullableTypeName(ITypeSymbol typeSymbol) => GetNonNullableTypeString(typeSymbol);

    /// <summary>
    /// Converts a type symbol to CLR type name format suitable for Type.GetType() and UnsafeAccessorType.
    /// For generic types, produces format like: "Namespace.Type`1[[TypeArg, Assembly]]"
    /// </summary>
    private static string? GetClrTypeName(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
        {
            return null; // Not needed for non-generic types
        }

        var sb = new StringBuilder();

        // Build the namespace and containing types
        if (typeSymbol.ContainingNamespace != null && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            sb.Append(typeSymbol.ContainingNamespace.ToDisplayString());
            sb.Append('.');
        }

        // Handle nested types
        var containingTypes = new Stack<INamedTypeSymbol>();
        var current = typeSymbol.ContainingType;
        while (current != null)
        {
            containingTypes.Push(current);
            current = current.ContainingType;
        }

        foreach (var containingType in containingTypes)
        {
            sb.Append(containingType.MetadataName);
            sb.Append('+');
        }

        // Add the type name with generic arity (e.g., "GenericType`1")
        sb.Append(typeSymbol.MetadataName);

        // Add type arguments in CLR format: [[TypeArg1, Assembly], [TypeArg2, Assembly]]
        if (typeSymbol.TypeArguments.Length > 0)
        {
            sb.Append('[');
            for (int i = 0; i < typeSymbol.TypeArguments.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('[');
                sb.Append(GetAssemblyQualifiedTypeName(typeSymbol.TypeArguments[i]));
                sb.Append(']');
            }
            sb.Append(']');
        }

        // Add assembly name for the containing type
        if (typeSymbol.ContainingAssembly != null)
        {
            sb.Append(", ");
            sb.Append(typeSymbol.ContainingAssembly.Name);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the assembly-qualified type name for a type symbol.
    /// </summary>
    private static string GetAssemblyQualifiedTypeName(ITypeSymbol typeSymbol)
    {
        var sb = new StringBuilder();

        // Handle generic types recursively
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            // Build namespace
            if (namedType.ContainingNamespace != null && !namedType.ContainingNamespace.IsGlobalNamespace)
            {
                sb.Append(namedType.ContainingNamespace.ToDisplayString());
                sb.Append('.');
            }

            // Handle nested types
            var containingTypes = new Stack<INamedTypeSymbol>();
            var current = namedType.ContainingType;
            while (current != null)
            {
                containingTypes.Push(current);
                current = current.ContainingType;
            }

            foreach (var containingType in containingTypes)
            {
                sb.Append(containingType.MetadataName);
                sb.Append('+');
            }

            sb.Append(namedType.MetadataName);

            // Add type arguments recursively
            if (namedType.TypeArguments.Length > 0)
            {
                sb.Append('[');
                for (int i = 0; i < namedType.TypeArguments.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('[');
                    sb.Append(GetAssemblyQualifiedTypeName(namedType.TypeArguments[i]));
                    sb.Append(']');
                }
                sb.Append(']');
            }
        }
        else if (typeSymbol is INamedTypeSymbol simpleNamedType)
        {
            // Build namespace
            if (simpleNamedType.ContainingNamespace != null && !simpleNamedType.ContainingNamespace.IsGlobalNamespace)
            {
                sb.Append(simpleNamedType.ContainingNamespace.ToDisplayString());
                sb.Append('.');
            }

            // Handle nested types
            var containingTypes = new Stack<INamedTypeSymbol>();
            var current = simpleNamedType.ContainingType;
            while (current != null)
            {
                containingTypes.Push(current);
                current = current.ContainingType;
            }

            foreach (var containingType in containingTypes)
            {
                sb.Append(containingType.MetadataName);
                sb.Append('+');
            }

            sb.Append(simpleNamedType.MetadataName);
        }
        else
        {
            // Fallback for other type kinds (arrays, pointers, etc.)
            sb.Append(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", ""));
        }

        // Add assembly name
        if (typeSymbol.ContainingAssembly != null)
        {
            sb.Append(", ");
            sb.Append(typeSymbol.ContainingAssembly.Name);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates type parameter constraints string (e.g., "where T : class" or "where T1 : class where T2 : struct, new()").
    /// </summary>
    private static string? GetTypeParameterConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var constraintParts = new List<string>();

        foreach (var tp in typeParameters)
        {
            var constraints = new List<string>();

            // Primary constraints (must come first)
            if (tp.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            else if (tp.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (tp.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }
            else if (tp.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }

            // Type constraints (base class and interfaces)
            foreach (var constraintType in tp.ConstraintTypes)
            {
                constraints.Add(constraintType.ToDisplayString());
            }

            // Constructor constraint (must come last)
            if (tp.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                constraintParts.Add($"where {tp.Name} : {string.Join(", ", constraints)}");
            }
        }

        return constraintParts.Count > 0 ? string.Join(" ", constraintParts) : null;
    }

    #endregion
}
