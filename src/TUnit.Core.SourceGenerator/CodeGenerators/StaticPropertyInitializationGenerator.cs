using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;
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

        // Only classes that can contribute a static data-source property are inspected semantically:
        // classes declaring an attributed static property, classes with a base list (which may inherit
        // such properties, including from other assemblies), and partial classes (whose other parts may
        // declare either). Every other class only walks to System.Object without finding anything.
        var classChains = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateClass(node),
                transform: static (ctx, ct) => GetStaticPropertyChain(ctx, ct))
            // Every chain contains the class itself, so drop chains without any static data-source
            // property: they add nothing to the result, and keeping them out of the collected array
            // stops unrelated classes (any class with a base list) from re-running the parse step.
            .Where(static chain => HasAnyProperty(chain));

        var testClasses = classChains
            .Collect()
            .Combine(enabledProvider)
            .Select(static (classesProviderPair, _) =>
                ParseStaticPropertyInitializers(classesProviderPair.Left, classesProviderPair.Right))
            .WithTrackingName(ParseStaticProperties);

        context.RegisterSourceOutput(testClasses, GenerateStaticPropertyInitialization);
    }

    /// <summary>
    /// The static data-source properties declared directly on one type of an inheritance chain.
    /// </summary>
    /// <param name="TypeKey">Identity of the type, matching <see cref="SymbolEqualityComparer.Default"/>.</param>
    /// <param name="Properties">The type's public static data-source properties, in member order.</param>
    internal sealed record StaticPropertyTypeSegment(string TypeKey, EquatableArray<StaticPropertyEntry> Properties);

    internal sealed record StaticPropertyEntry(string Name, PropertyWithDataSourceModel Model);

    // Base types are shared by many classes. Cache each type's segment per compilation so chain
    // walks read the members and attributes of a common base (or BCL) type only once.
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<INamedTypeSymbol, StaticPropertyTypeSegment>> SegmentCaches = new();

    private static bool IsCandidateClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        if (classDeclaration.BaseList is not null || classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return true;
        }

        foreach (var member in classDeclaration.Members)
        {
            if (member is PropertyDeclarationSyntax { AttributeLists.Count: > 0 } property
                && property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyProperty(EquatableArray<StaticPropertyTypeSegment> chain)
    {
        // Indexed loop: EquatableArray<T>.GetEnumerator() returns a boxed IEnumerator<T>.
        for (var i = 0; i < chain.Length; i++)
        {
            if (chain[i].Properties.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static EquatableArray<StaticPropertyTypeSegment> GetStaticPropertyChain(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return EquatableArray<StaticPropertyTypeSegment>.Empty;
        }

        // Skip open generic types - we can't generate code for types with unbound type parameters
        // The initialization will happen in the consuming assembly that provides concrete type arguments
        if (typeSymbol.IsGenericType && typeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))
        {
            return EquatableArray<StaticPropertyTypeSegment>.Empty;
        }

        var cache = SegmentCaches.GetValue(context.SemanticModel.Compilation,
            static _ => new ConcurrentDictionary<INamedTypeSymbol, StaticPropertyTypeSegment>(SymbolEqualityComparer.IncludeNullability));

        // Walk inheritance hierarchy to include base class static properties
        var segments = new List<StaticPropertyTypeSegment>();
        for (var currentType = typeSymbol; currentType != null; currentType = currentType.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!cache.TryGetValue(currentType, out var segment))
            {
                segment = cache.GetOrAdd(currentType, static type => CreateSegment(type));
            }

            segments.Add(segment);
        }

        return segments.ToEquatableArray();
    }

    private static StaticPropertyTypeSegment CreateSegment(INamedTypeSymbol type)
    {
        List<StaticPropertyEntry>? properties = null;

        foreach (var member in type.GetMembers())
        {
            if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: true } property) // Only static properties for session initialization
            {
                var dataSourceAttr = property.GetAttributes()
                    .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                if (dataSourceAttr != null)
                {
                    properties ??= [];
                    properties.Add(new StaticPropertyEntry(
                        property.Name,
                        ToPropertyWithDataSourceModel(new PropertyWithDataSource
                        {
                            Property = property,
                            DataSourceAttribute = dataSourceAttr
                        })));
                }
            }
        }

        return new StaticPropertyTypeSegment(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            properties is null ? EquatableArray<StaticPropertyEntry>.Empty : properties.ToEquatableArray());
    }

    private static EquatableArray<PropertyWithDataSourceModel> ParseStaticPropertyInitializers(ImmutableArray<EquatableArray<StaticPropertyTypeSegment>> classChains, bool enabledProvider)
    {
        if (!enabledProvider)
        {
            return EquatableArray<PropertyWithDataSourceModel>.Empty;
        }

        // Use a set to deduplicate static properties by their declaring type and name
        // This prevents duplicate initialization when derived classes inherit static properties
        var uniqueStaticProperties = new HashSet<(string DeclaringType, string Name)>();
        var walkPropertyNames = new HashSet<string>();
        var result = new List<PropertyWithDataSourceModel>();

        foreach (var chain in classChains)
        {
            walkPropertyNames.Clear();

            // Every chain is walked in full, even through types an earlier chain reached: a property
            // hidden by a derived type in one walk must still be added when its declaring type's own
            // chain is walked, whatever the order of the chains. uniqueStaticProperties dedupes.
            foreach (var segment in chain)
            {
                foreach (var property in segment.Properties)
                {
                    // Check if we already have this property (in case of overrides)
                    if (!walkPropertyNames.Add(property.Name))
                    {
                        continue;
                    }

                    // Static properties belong to their declaring type, not derived types
                    // Only add if we haven't seen this exact property before
                    if (uniqueStaticProperties.Add((segment.TypeKey, property.Name)))
                    {
                        result.Add(property.Model);
                    }
                }
            }
        }

        return result.ToEquatableArray();
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

        // Generate individual property initializer methods that return the value and set the property
        var generatedMethods = new HashSet<string>();
        foreach (var propertyData in staticProperties)
        {
            var methodName = GetInitializerMethodName(propertyData);
            if (generatedMethods.Add(methodName))
            {
                GenerateIndividualPropertyInitializer(writer, propertyData, methodName);
            }
        }

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        // Contribute static field initializers to the shared TUnit_StaticPropertyRegistration partial
        // (declared by InfrastructureGenerator). The compiler merges all contributions into one .cctor,
        // triggered once via RunClassConstructor — replacing the per-assembly [ModuleInitializer].
        writer.AppendLine("namespace TUnit.Generated");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("internal static partial class TUnit_StaticPropertyRegistration");
        writer.AppendLine("{");
        writer.Indent();

        var registeredFields = new HashSet<string>();
        foreach (var propertyData in staticProperties)
        {
            var typeName = propertyData.Property.ContainingType.GloballyQualified;
            var methodName = GetInitializerMethodName(propertyData);
            // SafeName maps every non-alphanumeric char to '_', so distinct types whose names differ
            // only in '.' vs '_' (e.g. A_B.C vs A.B_C) would collide. Append a deterministic hash of
            // the fully-qualified type + property to keep each merged-.cctor field unique. FNV-1a (not
            // string.GetHashCode) so the field name is stable across compiler restarts.
            var stableHash = GetStableHash(propertyData);
            var fieldName = $"_r_{FileNameHelper.SafeName(typeName)}_{propertyData.Property.Name}_{stableHash}";

            if (!registeredFields.Add(fieldName))
            {
                continue;
            }

            writer.AppendLine($"static readonly int {fieldName} = global::TUnit.Core.StaticProperties.StaticPropertyRegistry.Register(new global::TUnit.Core.StaticProperties.StaticPropertyMetadata");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"PropertyName = \"{propertyData.Property.Name}\",");
            writer.AppendLine($"PropertyType = typeof({propertyData.Property.GloballyQualifiedType}),");
            writer.AppendLine($"DeclaringType = typeof({typeName}),");
            writer.AppendLine($"InitializerAsync = global::TUnit.Core.Generated.StaticPropertyInitializer.{methodName}");
            writer.Unindent();
            writer.AppendLine("});");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");

        return writer.ToString();
    }

    // Stable FNV-1a hash of the fully-qualified type + property name. Shared by the initializer
    // method name and the registration field name so that distinct types which share a simple name
    // (e.g. Acme.Tests.MyFixture vs Acme.Shared.MyFixture) produce distinct, non-colliding methods.
    private static string GetStableHash(PropertyWithDataSourceModel propertyData)
    {
        var typeName = propertyData.Property.ContainingType.GloballyQualified;
        return FileNameHelper.GetStableHashCode($"{typeName}.{propertyData.Property.Name}").ToString("x8", CultureInfo.InvariantCulture);
    }

    private static string GetInitializerMethodName(PropertyWithDataSourceModel propertyData)
    {
        return $"Initialize_{propertyData.Property.ContainingType.Name}_{propertyData.Property.Name}_{GetStableHash(propertyData)}";
    }

    private static void GenerateIndividualPropertyInitializer(CodeWriter writer, PropertyWithDataSourceModel propertyData, string methodName)
    {
        var propertyName = propertyData.Property.Name;
        var typeName = propertyData.Property.ContainingType.GloballyQualified;

        writer.AppendLine();
        writer.AppendLine("/// <summary>");
        writer.AppendLine($"/// Initializer for {typeName}.{propertyName}");
        writer.AppendLine("/// </summary>");
        writer.AppendLine($"internal static async global::System.Threading.Tasks.Task<object?> {methodName}()");
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
            methodName = attr.ConstructorArguments[1].Value.ToInvariantString();
        }
        else
        {
            methodName = attr.ConstructorArguments[0].Value.ToInvariantString();
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
