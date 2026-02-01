using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class StaticPropertyInitializationGenerator : IIncrementalGenerator
{
    public const string ParseStaticProperties = "ParseStaticProperties";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var testClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx)
            .Collect()
            .Combine(enabledProvider)
            .Select((classesProviderPair, _) =>
                ParseStaticPropertyInitializers(classesProviderPair.Left, classesProviderPair.Right))
            .WithTrackingName(ParseStaticProperties);

        context.RegisterSourceOutput(testClasses, GenerateStaticPropertyInitialization);
    }

    private static EquatableArray<PropertyWithDataSourceModel> ParseStaticPropertyInitializers(ImmutableArray<GeneratorSyntaxContext> classesContext, bool enabledProvider)
    {
        if (!enabledProvider)
        {
            return EquatableArray<PropertyWithDataSourceModel>.Empty;
        }

        // Use a dictionary to deduplicate static properties by their declaring type and name
        // This prevents duplicate initialization when derived classes inherit static properties
        var uniqueStaticProperties = new Dictionary<(INamedTypeSymbol DeclaringType, string Name), PropertyWithDataSource>(SymbolEqualityComparer.Default.ToTupleComparer());
        var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var properties = new List<PropertyWithDataSource>();

        foreach (var context in classesContext)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            // Skip open generic types - we can't generate code for types with unbound type parameters
            // The initialization will happen in the consuming assembly that provides concrete type arguments
            if (typeSymbol.IsGenericType && typeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))
            {
                continue;
            }

            // Check if this type has any static properties with data source attributes
            foreach (var prop in GetStaticPropertyDataSources(typeSymbol, visitedTypes, properties))
            {
                // Static properties belong to their declaring type, not derived types
                // Only add if we haven't seen this exact property before
                var key = (prop.Property.ContainingType, prop.Property.Name);
                if (!uniqueStaticProperties.ContainsKey(key))
                {
                    uniqueStaticProperties[key] = prop;
                }
            }
        }

        return uniqueStaticProperties.Values.Select(ToPropertyWithDataSourceModel).ToEquatableArray();
    }

    private static List<PropertyWithDataSource> GetStaticPropertyDataSources(
        INamedTypeSymbol typeSymbol,
        HashSet<INamedTypeSymbol> visitedType,
        List<PropertyWithDataSource> properties
        )
    {
        properties.Clear();

        // Walk inheritance hierarchy to include base class static properties
        var currentType = typeSymbol;
        while (currentType != null)
        {
            if (!visitedType.Add(currentType))
            {
                break;
            }

            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: true } property) // Only static properties for session initialization
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        // Check if we already have this property (in case of overrides)
                        bool newProperty = true;
                        foreach (var p in properties)
                        {
                            if (p.Property.Name == property.Name)
                            {
                                newProperty = false;
                                break;
                            }
                        }

                        if (newProperty)
                        {
                            properties.Add(new PropertyWithDataSource
                            {
                                Property = property,
                                DataSourceAttribute = dataSourceAttr
                            });
                        }
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        return properties;
    }

    private static PropertyWithDataSourceModel ToPropertyWithDataSourceModel(PropertyWithDataSource staticProperty)
    {
        var containingType = new ContainingType(
            staticProperty.Property.ContainingType.GloballyQualified(),
            staticProperty.Property.ContainingType.Name,
            staticProperty.Property.ContainingType.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            staticProperty.Property.ContainingType.ContainingAssembly.Name
        );

        var property = new PropertyType(
            staticProperty.Property.Type.GloballyQualified(),
            staticProperty.Property.Name,
            containingType
        );

        var attr = staticProperty.DataSourceAttribute;
        var attributeClassName = attr.AttributeClass?.Name;

        DataSourceAttribute sourceAttribute;

        // Generate data source logic based on attribute type
        if (attributeClassName == "ArgumentsAttribute")
        {
            sourceAttribute = ParseArgumentsDataSourceWithAssignment(attr);;
        }
        else if (attributeClassName == "MethodDataSourceAttribute")
        {
            sourceAttribute = ParseMethodDataSourceWithAssignment(attr, staticProperty.Property.ContainingType);
        }
        else if (attr.AttributeClass?.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") == true ||
                 attr.AttributeClass?.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute") == true)
        {
            sourceAttribute = new DataSourceAttribute.AsyncDataSource(CodeGenerationHelpers.GenerateAttributeInstantiation(attr));
        }
        else
        {
            sourceAttribute = new DataSourceAttribute.Fallback();
        }

        return new PropertyWithDataSourceModel(property, sourceAttribute);
    }

    private static void GenerateStaticPropertyInitialization(SourceProductionContext context, EquatableArray<PropertyWithDataSourceModel> testClasses)
    {
        if (testClasses.Length == 0)
        {
            return;
        }

        var code = GenerateInitializationCode(testClasses);
        context.AddSource("StaticPropertyInitializer.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateInitializationCode(EquatableArray<PropertyWithDataSourceModel> staticProperties)
    {
        using var writer = new CodeWriter();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using TUnit.Core;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Core.Generated");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Auto-generated static property initializer");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("internal static class StaticPropertyInitializer");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Module initializer that registers static property metadata");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();

        // Register each property with metadata
        foreach (var propertyData in staticProperties)
        {
            var typeName = propertyData.Property.ContainingType.GloballyQualified;
            var methodName = $"Initialize_{propertyData.Property.ContainingType.Name}_{propertyData.Property.Name}";

            writer.AppendLine("global::TUnit.Core.StaticProperties.StaticPropertyRegistry.Register(new global::TUnit.Core.StaticProperties.StaticPropertyMetadata");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"PropertyName = \"{propertyData.Property.Name}\",");
            writer.AppendLine($"PropertyType = typeof({propertyData.Property.GloballyQualifiedType}),");
            writer.AppendLine($"DeclaringType = typeof({typeName}),");
            writer.AppendLine($"InitializerAsync = {methodName}");
            writer.Unindent();
            writer.AppendLine("});");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        // Generate individual property initializer methods that return the value and set the property
        var generatedMethods = new HashSet<string>();
        foreach (var propertyData in staticProperties)
        {
            var methodName = $"Initialize_{propertyData.Property.ContainingType.Name}_{propertyData.Property.Name}";
            if (generatedMethods.Add(methodName))
            {
                GenerateIndividualPropertyInitializer(writer, propertyData);
            }
        }

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");

        return writer.ToString();
    }

    private static void GenerateIndividualPropertyInitializer(CodeWriter writer, PropertyWithDataSourceModel propertyData)
    {
        var propertyName = propertyData.Property.Name;
        var typeName = propertyData.Property.ContainingType.GloballyQualified;
        var methodName = $"Initialize_{propertyData.Property.ContainingType.Name}_{propertyName}";

        writer.AppendLine();
        writer.AppendLine("/// <summary>");
        writer.AppendLine($"/// Initializer for {typeName}.{propertyName}");
        writer.AppendLine("/// </summary>");
        writer.AppendLine($"private static async global::System.Threading.Tasks.Task<object?> {methodName}()");
        writer.AppendLine("{");
        writer.Indent();

        // Create PropertyMetadata with containing type information
        writer.AppendLine($"// Create PropertyMetadata for {propertyName}");
        writer.AppendLine("var containingTypeMetadata = new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"Name = \"{propertyData.Property.ContainingType.Name}\",");
        writer.AppendLine($"Type = typeof({typeName}),");
        writer.AppendLine($"Namespace = \"{propertyData.Property.ContainingType.ContainingNamespace}\",");
        writer.AppendLine($"TypeInfo = new global::TUnit.Core.ConcreteType(typeof({typeName})),");
        writer.AppendLine($"Assembly = global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{propertyData.Property.ContainingType.ContainingAssemblyName}\", () => new global::TUnit.Core.AssemblyMetadata {{ Name = \"{propertyData.Property.ContainingType.ContainingAssemblyName}\" }}),");
        writer.AppendLine("Properties = global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>(),");
        writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        writer.AppendLine("Parent = null");
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        writer.AppendLine("var propertyMetadata = new global::TUnit.Core.PropertyMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"Name = \"{propertyName}\",");
        writer.AppendLine($"Type = typeof({propertyData.Property.GloballyQualifiedType}),");
        writer.AppendLine($"IsStatic = true,");
        writer.AppendLine($"ReflectionInfo = typeof({typeName}).GetProperty(\"{propertyName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static),");
        writer.AppendLine($"Getter = _ => {typeName}.{propertyName},");
        writer.AppendLine("ClassMetadata = containingTypeMetadata,");
        writer.AppendLine("ContainingTypeMetadata = containingTypeMetadata");
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        var attr = propertyData.DataSourceAttribute;

        // Generate data source logic and capture the value
        writer.AppendLine("object? value = null;");
        writer.AppendLine();

        // Generate data source logic based on attribute type
        if (attr is DataSourceAttribute.ArgumentsDataSource argumentsDataSource)
        {
            if (argumentsDataSource.FormattedValue is not null)
            {
                writer.AppendLine($"value = {argumentsDataSource.FormattedValue};");
            }
        }
        else if (attr is DataSourceAttribute.MethodDataSource methodDataSource)
        {
            if (methodDataSource.Data is not null)
            {
                writer.AppendLine($"var data = {methodDataSource.Data}();");
                writer.AppendLine("value = await global::TUnit.Core.Helpers.DataSourceHelpers.ProcessDataSourceResultGeneric(data);");
            }
        }
        else if (attr is DataSourceAttribute.AsyncDataSource asyncDataSource)
        {
            GenerateAsyncDataSourceGeneratorWithPropertyWithAssignment(writer, asyncDataSource);
        }
        else
        {
            writer.AppendLine("// Unsupported data source attribute");
        }

        writer.AppendLine();
        writer.AppendLine("// Set the property value if we got one");
        writer.AppendLine("if (value != null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"{typeName}.{propertyName} = ({propertyData.Property.GloballyQualifiedType})value;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("return value;");

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static readonly TypedConstantFormatter _formatter = new();

    private static DataSourceAttribute ParseArgumentsDataSourceWithAssignment(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length > 0)
        {
            var argValue = attr.ConstructorArguments[0];

            // ArgumentsAttribute constructor takes params object?[], so the argument is always an array
            if (argValue is { Kind: TypedConstantKind.Array, Values.Length: > 0 })
            {
                // For static property injection, we only use the first value from the array
                var firstValue = argValue.Values[0];
                var formattedValue = _formatter.FormatForCode(firstValue);
                return new DataSourceAttribute.ArgumentsDataSource(formattedValue);
            }
        }

        return new DataSourceAttribute.ArgumentsDataSource(null);
    }

    private static DataSourceAttribute ParseMethodDataSourceWithAssignment(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length < 1)
        {
            return new DataSourceAttribute.MethodDataSource(null);
        }

        string? methodName = null;
        ITypeSymbol? targetType = null;

        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol type } _, _
            ])
        {
            targetType = type;
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else
        {
            methodName = attr.ConstructorArguments[0].Value?.ToString();
            targetType = containingType;
        }

        if (string.IsNullOrEmpty(methodName) || targetType == null)
        {
            return new DataSourceAttribute.MethodDataSource(null);
        }

        var fullyQualifiedType = targetType.GloballyQualified();
        return new DataSourceAttribute.MethodDataSource($"{fullyQualifiedType}.{methodName}");
    }

    private static void GenerateAsyncDataSourceGeneratorWithPropertyWithAssignment(CodeWriter writer, DataSourceAttribute.AsyncDataSource attr)
    {
        writer.AppendLine($"var generator = {attr.GeneratedCode};");
        writer.AppendLine("// Use the global static property context for disposal tracking");
        writer.AppendLine("var globalContext = global::TUnit.Core.TestSessionContext.GlobalStaticPropertyContext;");
        writer.AppendLine("var metadata = new global::TUnit.Core.DataGeneratorMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Type = global::TUnit.Core.Enums.DataGeneratorType.Property,");
        writer.AppendLine("TestBuilderContext = new global::TUnit.Core.TestBuilderContextAccessor(globalContext),");
        writer.AppendLine("MembersToGenerate = new global::TUnit.Core.IMemberMetadata[] { propertyMetadata },");
        writer.AppendLine("TestInformation = null,");
        writer.AppendLine("TestSessionId = global::TUnit.Core.TestSessionContext.Current?.Id ?? \"static-property-init\",");
        writer.AppendLine("TestClassInstance = null,");
        writer.AppendLine("ClassInstanceArguments = null");
        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("await foreach (var dataSourceFunc in ((global::TUnit.Core.IDataSourceAttribute)generator).GetDataRowsAsync(metadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var data = await dataSourceFunc();");
        writer.AppendLine("if (data?.Length > 0)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("value = data[0];");
        writer.AppendLine("break;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }
}
