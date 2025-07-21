using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public sealed class PropertyInjectionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes that have properties with IDataSourceAttribute
        var classesWithPropertyInjection = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsClassWithDataSourceProperties(node),
                transform: (ctx, _) => GetClassWithDataSourceProperties(ctx))
            .Where(x => x != null)
            .Select((x, _) => x!);

        // Collect all discovered classes
        var collectedClasses = classesWithPropertyInjection.Collect();

        // Generate property injection sources
        context.RegisterSourceOutput(collectedClasses, GeneratePropertyInjectionSources);
    }

    private static bool IsClassWithDataSourceProperties(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Look for properties with attributes that might be data sources
        return classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(prop => prop.AttributeLists.Count > 0);
    }

    private static ClassWithDataSourceProperties? GetClassWithDataSourceProperties(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        
        var typeSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (typeSymbol == null || typeSymbol.IsAbstract)
            return null;

        // Find properties with IDataSourceAttribute, including inherited ones
        var propertiesWithDataSources = new List<PropertyWithDataSourceAttribute>();
        var dataSourceInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");
        
        if (dataSourceInterface == null)
            return null;
        
        // Traverse the inheritance chain to find all properties with data sources
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>(); // Track property names to avoid duplicates due to overrides
        
        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol property && CanSetProperty(property))
                {
                    // Skip if we've already processed a property with this name (due to override/new)
                    if (!processedProperties.Add(property.Name))
                        continue;
                        
                    foreach (var attr in property.GetAttributes())
                    {
                        if (attr.AttributeClass != null && 
                            attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default))
                        {
                            propertiesWithDataSources.Add(new PropertyWithDataSourceAttribute
                            {
                                Property = property,
                                DataSourceAttribute = attr
                            });
                            break; // Only one data source per property
                        }
                    }
                }
            }
            
            // Move up the inheritance chain
            currentType = currentType.BaseType;
            
            // Stop at System.Object or if we hit a null base type
            if (currentType?.SpecialType == SpecialType.System_Object)
                break;
        }

        if (propertiesWithDataSources.Count == 0)
            return null;

        return new ClassWithDataSourceProperties
        {
            ClassSymbol = typeSymbol,
            Properties = propertiesWithDataSources.ToImmutableArray()
        };
    }

    private static bool CanSetProperty(IPropertySymbol property)
    {
        // Can set if it has a setter OR if it's init-only (we'll use UnsafeAccessor)
        return property.SetMethod != null || property.SetMethod?.IsInitOnly == true;
    }

    private static void GeneratePropertyInjectionSources(SourceProductionContext context, ImmutableArray<ClassWithDataSourceProperties> classes)
    {
        if (classes.IsEmpty)
            return;

        var sourceBuilder = new StringBuilder();
        
        WriteFileHeader(sourceBuilder);
        
        // Generate module initializer to register sources
        GenerateModuleInitializer(sourceBuilder, classes);
        
        // Generate IPropertySource implementations
        foreach (var classInfo in classes)
        {
            GeneratePropertySource(sourceBuilder, classInfo);
        }


        context.AddSource("PropertyInjectionSources.g.cs", sourceBuilder.ToString());
    }

    private static void WriteFileHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using TUnit.Core;");
        sb.AppendLine("using TUnit.Core.Interfaces.SourceGenerator;");
        sb.AppendLine("using TUnit.Core.ReferenceTracking;");
        sb.AppendLine("using TUnit.Core.Enums;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Core;");
        sb.AppendLine();
    }

    private static void GenerateModuleInitializer(StringBuilder sb, ImmutableArray<ClassWithDataSourceProperties> classes)
    {
        sb.AppendLine("internal static class PropertyInjectionInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    // Module initializer to register property injection sources");
        sb.AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void InitializePropertyInjectionSources()");
        sb.AppendLine("    {");
        
        foreach (var classInfo in classes)
        {
            var sourceClassName = GetPropertySourceClassName(classInfo.ClassSymbol);
            sb.AppendLine($"        PropertySourceRegistry.Register(typeof({classInfo.ClassSymbol.ToDisplayString()}), new {sourceClassName}());");
        }
        
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Add helper methods
        GenerateHelperMethods(sb);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GeneratePropertySource(StringBuilder sb, ClassWithDataSourceProperties classInfo)
    {
        var sourceClassName = GetPropertySourceClassName(classInfo.ClassSymbol);
        var classTypeName = classInfo.ClassSymbol.ToDisplayString();

        sb.AppendLine($"internal sealed class {sourceClassName} : IPropertySource");
        sb.AppendLine("{");
        sb.AppendLine($"    public Type Type => typeof({classTypeName});");
        sb.AppendLine($"    public string PropertyName => \"{string.Join(", ", classInfo.Properties.Select(p => p.Property.Name))}\";");
        sb.AppendLine("    public bool ShouldInitialize => true;");
        sb.AppendLine();

        // Generate UnsafeAccessor methods for init-only properties
        GenerateUnsafeAccessorMethods(sb, classInfo);

        // Generate InitializeAsync method
        GenerateInitializeAsync(sb, classInfo, classTypeName);

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateUnsafeAccessorMethods(StringBuilder sb, ClassWithDataSourceProperties classInfo)
    {
        foreach (var propInfo in classInfo.Properties)
        {
            if (propInfo.Property.SetMethod?.IsInitOnly == true)
            {
                var propertyType = propInfo.Property.Type.ToDisplayString();
                var backingFieldName = $"<{propInfo.Property.Name}>k__BackingField";
                
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    private static extern ref {propertyType} Get{propInfo.Property.Name}BackingField({classInfo.ClassSymbol.ToDisplayString()} instance);");
                sb.AppendLine("#endif");
                sb.AppendLine();
            }
        }
    }

    private static void GenerateInitializeAsync(StringBuilder sb, ClassWithDataSourceProperties classInfo, string classTypeName)
    {
        sb.AppendLine("    public async Task InitializeAsync(object instance)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var typedInstance = ({classTypeName})instance;");
        sb.AppendLine("        var testContext = TestContext.Current;");
        sb.AppendLine("        if (testContext == null) return;");
        sb.AppendLine();

        sb.AppendLine("        var dataGeneratorMetadata = new DataGeneratorMetadata");
        sb.AppendLine("        {");
        sb.AppendLine("            TestBuilderContext = new TestBuilderContextAccessor(TestBuilderContext.Current ?? new TestBuilderContext()),");
        sb.AppendLine("            MembersToGenerate = Array.Empty<MemberMetadata>(),");
        sb.AppendLine("            TestInformation = testContext.TestDetails.MethodMetadata,");
        sb.AppendLine("            Type = DataGeneratorType.Property,");
        sb.AppendLine("            TestSessionId = testContext.TestDetails.TestId,");
        sb.AppendLine("            TestClassInstance = instance,");
        sb.AppendLine("            ClassInstanceArguments = testContext.TestDetails.TestClassArguments");
        sb.AppendLine("        };");
        sb.AppendLine();

        // Generate property injection for each property
        foreach (var propInfo in classInfo.Properties)
        {
            GeneratePropertyInjectionCode(sb, propInfo, classInfo.ClassSymbol);
        }

        sb.AppendLine("    }");
    }

    private static void GeneratePropertyInjectionCode(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, INamedTypeSymbol classSymbol)
    {
        var propertyName = propInfo.Property.Name;
        var propertyType = propInfo.Property.Type.ToDisplayString();
        var attributeTypeName = propInfo.DataSourceAttribute.AttributeClass!.ToDisplayString();

        sb.AppendLine($"        // Inject {propertyName} property");
        sb.AppendLine("        {");
        
        // Create attribute instance with constructor arguments
        GenerateAttributeInstantiation(sb, propInfo.DataSourceAttribute, attributeTypeName);
        
        sb.AppendLine("            var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);");
        sb.AppendLine();
        sb.AppendLine("            await foreach (var factory in dataRows)");
        sb.AppendLine("            {");
        sb.AppendLine("                var args = await factory();");
        sb.AppendLine("                var value = args?.FirstOrDefault();");
        sb.AppendLine();
        sb.AppendLine("                // Resolve Func<T> values to actual values");
        sb.AppendLine("                value = await PropertyInjectionInitializer.ResolveTestDataValueAsync(value);");
        sb.AppendLine();
        sb.AppendLine($"                if (value != null)");
        sb.AppendLine("                {");
        
        // Set property value (check if it's static)
        if (propInfo.Property.IsStatic)
        {
            GenerateStaticPropertySetting(sb, propInfo, propertyType);
        }
        else
        {
            GeneratePropertySetting(sb, propInfo, propertyType);
        }
        
        // Handle tracking differently for static vs instance properties
        if (propInfo.Property.IsStatic)
        {
            // For static properties, don't track since they're not per-instance
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("                    // Track the value for disposal/cleanup");
            sb.AppendLine("                    var trackedValue = DataSourceReferenceTrackerProvider.TrackDataSourceObject(value);");
            
            // Update property with tracked value if needed
            if (propInfo.Property.SetMethod?.IsInitOnly == true)
            {
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"                    Get{propInfo.Property.Name}BackingField(typedInstance) = ({propertyType})trackedValue;");
                sb.AppendLine("#endif");
            }
            else
            {
                sb.AppendLine($"                    typedInstance.{propertyName} = ({propertyType})trackedValue;");
            }
        }
        
        sb.AppendLine();
        
        // Recursive property injection
        GenerateRecursiveInjection(sb, propInfo, propertyType);
        
        sb.AppendLine("                    break; // Only take first value");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateAttributeInstantiation(StringBuilder sb, AttributeData attributeData, string attributeTypeName)
    {
        var constructorArgs = string.Join(", ", attributeData.ConstructorArguments.Select(FormatTypedConstant));
        
        sb.AppendLine($"            var dataSource = new {attributeTypeName}({constructorArgs});");
        
        // Handle named arguments if any
        foreach (var namedArg in attributeData.NamedArguments)
        {
            var value = FormatTypedConstant(namedArg.Value);
            sb.AppendLine($"            dataSource.{namedArg.Key} = {value};");
        }
    }

    private static void GeneratePropertySetting(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, string propertyType)
    {
        if (propInfo.Property.SetMethod?.IsInitOnly == true)
        {
            sb.AppendLine("#if NET8_0_OR_GREATER");
            sb.AppendLine($"                    Get{propInfo.Property.Name}BackingField(typedInstance) = ({propertyType})value;");
            sb.AppendLine("#else");
            sb.AppendLine($"                    // Fallback for init-only properties in older .NET");
            sb.AppendLine($"                    var backingField = typeof({propInfo.Property.ContainingType.ToDisplayString()}).GetField(\"<{propInfo.Property.Name}>k__BackingField\",");
            sb.AppendLine("                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);");
            sb.AppendLine("                    backingField?.SetValue(typedInstance, value);");
            sb.AppendLine("#endif");
        }
        else
        {
            sb.AppendLine($"                    typedInstance.{propInfo.Property.Name} = ({propertyType})value;");
        }
    }
    
    private static void GenerateStaticPropertySetting(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, string propertyType)
    {
        var className = propInfo.Property.ContainingType.ToDisplayString();
        sb.AppendLine($"                    {className}.{propInfo.Property.Name} = ({propertyType})value;");
    }

    private static void GenerateRecursiveInjection(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, string propertyType)
    {
        // Only do recursive injection for class types (not primitives, strings, etc.)
        if (propInfo.Property.Type.TypeKind == TypeKind.Class && 
            !propInfo.Property.Type.SpecialType.ToString().Contains("String"))
        {
            sb.AppendLine("                    // Recursively inject nested properties");
            
            // Get the non-nullable type name for typeof operator
            var nonNullableTypeName = GetNonNullableTypeString(propInfo.Property.Type);
            sb.AppendLine($"                    var nestedSource = PropertySourceRegistry.GetSource(typeof({nonNullableTypeName}));");
            sb.AppendLine("                    if (nestedSource != null)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        await nestedSource.InitializeAsync(value);");
            sb.AppendLine("                    }");
        }
    }


    private static void GenerateHelperMethods(StringBuilder sb)
    {
        sb.AppendLine("    // Helper method to resolve Func<T> values");
        sb.AppendLine("    public static async Task<object?> ResolveTestDataValueAsync(object? value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value == null) return null;");
        sb.AppendLine();
        sb.AppendLine("        var type = value.GetType();");
        sb.AppendLine();
        sb.AppendLine("        // Check if it's a Func<T>");
        sb.AppendLine("        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))");
        sb.AppendLine("        {");
        sb.AppendLine("            var invokeMethod = type.GetMethod(\"Invoke\");");
        sb.AppendLine("            var result = invokeMethod!.Invoke(value, null);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
    }

    private static string GetPropertySourceClassName(INamedTypeSymbol classSymbol)
    {
        var typeName = classSymbol.ToDisplayString().Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("+", "_");
        var hash = Math.Abs(typeName.GetHashCode()).ToString("x8");
        return $"{typeName}_PropertyInjectionSource_{hash}";
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        return constant.Kind switch
        {
            TypedConstantKind.Primitive when constant.Value is string str => $"\"{str}\"",
            TypedConstantKind.Primitive => constant.Value?.ToString() ?? "null",
            TypedConstantKind.Enum => FormatEnumConstant(constant),
            TypedConstantKind.Type => FormatTypeConstant(constant),
            TypedConstantKind.Array => $"new object[] {{ {string.Join(", ", constant.Values.Select(FormatTypedConstant))} }}",
            _ => constant.Value?.ToString() ?? "null"
        };
    }
    
    private static string FormatEnumConstant(TypedConstant constant)
    {
        if (constant.Type != null && constant.Value != null)
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
            // Get the non-nullable version of the type for typeof operator
            var displayString = GetNonNullableTypeString(typeSymbol);
            return $"typeof({displayString})";
        }
        
        return $"typeof({constant.Value})";
    }
    
    private static string GetNonNullableTypeString(ITypeSymbol typeSymbol)
    {
        // Handle nullable reference types
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // For nullable reference types, get the underlying type
            if (typeSymbol is INamedTypeSymbol { IsReferenceType: true })
            {
                return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();
            }
        }
        
        // Handle nullable value types like int?
        if (typeSymbol is INamedTypeSymbol namedType && 
            namedType.IsGenericType && 
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }
        
        var displayString = typeSymbol.ToDisplayString();
        
        // Fallback: remove ? suffix if present
        if (displayString.EndsWith("?"))
        {
            displayString = displayString.TrimEnd('?');
        }
        
        return displayString;
    }
}

// Supporting classes
internal sealed class ClassWithDataSourceProperties
{
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required ImmutableArray<PropertyWithDataSourceAttribute> Properties { get; init; }
}

internal sealed class PropertyWithDataSourceAttribute
{
    public required IPropertySymbol Property { get; init; }
    public required AttributeData DataSourceAttribute { get; init; }
}