using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public sealed class PropertyInjectionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classesWithPropertyInjection = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsClassWithDataSourceProperties(node),
                transform: (ctx, _) => GetClassWithDataSourceProperties(ctx))
            .Where(x => x != null)
            .Select((x, _) => x!);

        var collectedClasses = classesWithPropertyInjection.Collect();

        context.RegisterSourceOutput(collectedClasses, GeneratePropertyInjectionSources);
    }

    private static bool IsClassWithDataSourceProperties(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax;
    }

    private static ClassWithDataSourceProperties? GetClassWithDataSourceProperties(GeneratorSyntaxContext context)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol || typeSymbol.IsAbstract || typeSymbol.IsStatic)
        {
            return null;
        }

        // Skip types that are not publicly accessible to avoid accessibility issues
        // Also check if the type is nested and ensure the containing types are also public
        if (!IsPubliclyAccessible(typeSymbol))
        {
            return null;
        }

        // Skip open generic types (unbound type parameters) as they cannot be instantiated
        if (typeSymbol.IsUnboundGenericType || typeSymbol.TypeParameters.Length > 0)
        {
            return null;
        }

        var propertiesWithDataSources = new List<PropertyWithDataSourceAttribute>();
        var dataSourceInterface = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");

        if (dataSourceInterface == null)
        {
            return null;
        }

        // Check if this type itself implements IDataSourceAttribute (for custom data source classes)
        var implementsDataSource = typeSymbol.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default);

        var processedProperties = new HashSet<string>();

        var properties = typeSymbol.GetMembersIncludingBase()
            .OfType<IPropertySymbol>()
            .Where(CanSetProperty);

        foreach (var property in properties)
        {
            if (!processedProperties.Add(property.Name))
            {
                continue;
            }

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

        return new ClassWithDataSourceProperties
        {
            ClassSymbol = typeSymbol,
            Properties = propertiesWithDataSources.ToImmutableArray()
        };
    }

    private static bool CanSetProperty(IPropertySymbol property)
    {
        return property.SetMethod != null || property.SetMethod?.IsInitOnly == true;
    }

    private static bool IsPubliclyAccessible(INamedTypeSymbol typeSymbol)
    {
        // Check if the type itself is public
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        // If it's a nested type, ensure all containing types are also public
        // and don't have unbound type parameters
        var containingType = typeSymbol.ContainingType;
        while (containingType != null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            // Check if the containing type has unbound type parameters
            if (containingType.IsUnboundGenericType || containingType.TypeParameters.Length > 0)
            {
                return false;
            }

            containingType = containingType.ContainingType;
        }

        return true;
    }

    private static void GeneratePropertyInjectionSources(SourceProductionContext context, ImmutableArray<ClassWithDataSourceProperties> classes)
    {
        if (classes.IsEmpty)
        {
            return;
        }

        var sourceBuilder = new StringBuilder();

        WriteFileHeader(sourceBuilder);

        // Deduplicate classes by symbol to prevent duplicate source generation
        var uniqueClasses = classes
            .GroupBy(c => c.ClassSymbol, SymbolEqualityComparer.Default)
            .Select(g => g.First())
            .ToImmutableArray();

        // Generate all property sources first with stable names
        var classNameMapping = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        foreach (var classInfo in uniqueClasses)
        {
            var sourceClassName = GetPropertySourceClassName(classInfo.ClassSymbol);
            classNameMapping[classInfo.ClassSymbol] = sourceClassName;
        }

        GenerateModuleInitializer(sourceBuilder, uniqueClasses, classNameMapping);

        foreach (var classInfo in uniqueClasses)
        {
            GeneratePropertySource(sourceBuilder, classInfo, classNameMapping[classInfo.ClassSymbol]);
        }


        context.AddSource("PropertyInjectionSources.g.cs", sourceBuilder.ToString());
    }

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

    private static void GenerateModuleInitializer(StringBuilder sb, ImmutableArray<ClassWithDataSourceProperties> classes, Dictionary<INamedTypeSymbol, string> classNameMapping)
    {
        sb.AppendLine("internal static class PropertyInjectionInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void InitializePropertyInjectionSources()");
        sb.AppendLine("    {");

        foreach (var classInfo in classes)
        {
            var sourceClassName = classNameMapping[classInfo.ClassSymbol];
            var classTypeName = classInfo.ClassSymbol.GloballyQualified();
            sb.AppendLine($"        PropertySourceRegistry.Register(typeof({classTypeName}), new {sourceClassName}());");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GeneratePropertySource(StringBuilder sb, ClassWithDataSourceProperties classInfo, string sourceClassName)
    {
        var classTypeName = classInfo.ClassSymbol.GloballyQualified();

        sb.AppendLine($"internal sealed class {sourceClassName} : IPropertySource");
        sb.AppendLine("{");
        sb.AppendLine($"    public Type Type => typeof({classTypeName});");
        sb.AppendLine("    public bool ShouldInitialize => true;");
        sb.AppendLine();

        GenerateUnsafeAccessorMethods(sb, classInfo);

        GenerateGetPropertyMetadata(sb, classInfo, classTypeName);

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

                // Use the property's containing type for the UnsafeAccessor, not the derived class
                var containingType = propInfo.Property.ContainingType.ToDisplayString();

                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                sb.AppendLine($"    private static extern ref {propertyType} Get{propInfo.Property.Name}BackingField({containingType} instance);");
                sb.AppendLine("#endif");
                sb.AppendLine();
            }
        }
    }

    private static void GenerateGetPropertyMetadata(StringBuilder sb, ClassWithDataSourceProperties classInfo, string classTypeName)
    {
        sb.AppendLine("    public IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata()");
        sb.AppendLine("    {");

        if (classInfo.Properties.Length == 0)
        {
            sb.AppendLine("        yield break;");
        }
        else
        {
            foreach (var propInfo in classInfo.Properties)
            {
                GeneratePropertyMetadata(sb, propInfo, classInfo.ClassSymbol, classTypeName);
            }
        }

        sb.AppendLine("    }");
    }

    private static void GeneratePropertyMetadata(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, INamedTypeSymbol classSymbol, string classTypeName)
    {
        var propertyName = propInfo.Property.Name;
        var propertyType = propInfo.Property.Type.ToDisplayString();
        var propertyTypeForTypeof = GetNonNullableTypeString(propInfo.Property.Type);
        var attributeTypeName = propInfo.DataSourceAttribute.AttributeClass!.ToDisplayString();

        sb.AppendLine("        yield return new PropertyInjectionMetadata");
        sb.AppendLine("        {");
        sb.AppendLine($"            PropertyName = \"{propertyName}\",");
        sb.AppendLine($"            PropertyType = typeof({propertyTypeForTypeof}),");
        sb.AppendLine($"            ContainingType = typeof({classSymbol.ToDisplayString()}),");

        // Generate CreateDataSource delegate
        sb.AppendLine("            CreateDataSource = () =>");
        sb.AppendLine("            {");
        GenerateDataSourceCreation(sb, propInfo.DataSourceAttribute, attributeTypeName);
        sb.AppendLine("            },");

        // Generate SetProperty delegate
        sb.AppendLine("            SetProperty = (instance, value) =>");
        sb.AppendLine("            {");
        sb.AppendLine($"                var typedInstance = ({classTypeName})instance;");
        GeneratePropertySetting(sb, propInfo, propertyType, "typedInstance", classTypeName);
        sb.AppendLine("            }");

        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private static void GenerateDataSourceCreation(StringBuilder sb, AttributeData attributeData, string attributeTypeName)
    {
        var constructorArgs = string.Join(", ", attributeData.ConstructorArguments.Select(FormatTypedConstant));

        sb.AppendLine($"                var dataSource = new {attributeTypeName}({constructorArgs});");

        foreach (var namedArg in attributeData.NamedArguments)
        {
            var value = FormatTypedConstant(namedArg.Value);
            sb.AppendLine($"                dataSource.{namedArg.Key} = {value};");
        }

        sb.AppendLine("                return dataSource;");
    }

    private static void GeneratePropertySetting(StringBuilder sb, PropertyWithDataSourceAttribute propInfo, string propertyType, string instanceVariableName, string classTypeName)
    {
        if (propInfo.Property.SetMethod?.IsInitOnly == true)
        {
            var castExpression = GetPropertyCastExpression(propInfo.Property, propertyType);

            sb.AppendLine("#if NET8_0_OR_GREATER");
            // Cast to the property's containing type if needed
            var containingType = propInfo.Property.ContainingType.ToDisplayString();
            if (containingType != classTypeName)
            {
                sb.AppendLine($"                Get{propInfo.Property.Name}BackingField(({containingType}){instanceVariableName}) = {castExpression};");
            }
            else
            {
                sb.AppendLine($"                Get{propInfo.Property.Name}BackingField({instanceVariableName}) = {castExpression};");
            }
            sb.AppendLine("#else");
            sb.AppendLine($"                var backingField = typeof({propInfo.Property.ContainingType.ToDisplayString()}).GetField(\"<{propInfo.Property.Name}>k__BackingField\",");
            sb.AppendLine("                    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
            sb.AppendLine($"                backingField?.SetValue({instanceVariableName}, value);");
            sb.AppendLine("#endif");
        }
        else if (propInfo.Property.IsStatic)
        {
            var className = propInfo.Property.ContainingType.ToDisplayString();
            var castExpression = GetPropertyCastExpression(propInfo.Property, propertyType);
            sb.AppendLine($"                {className}.{propInfo.Property.Name} = {castExpression};");
        }
        else
        {
            var castExpression = GetPropertyCastExpression(propInfo.Property, propertyType);
            sb.AppendLine($"                {instanceVariableName}.{propInfo.Property.Name} = {castExpression};");
        }
    }

    private static string GetPropertyCastExpression(IPropertySymbol property, string propertyType)
    {
        var isNullableValueType = property.Type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };

        if (property.Type.IsValueType && !isNullableValueType)
        {
            return $"({propertyType})value!";
        }

        return $"({propertyType})value";
    }


    private static string GetPropertySourceClassName(INamedTypeSymbol classSymbol)
    {
        // Use a deterministic hash based on the fully qualified type name for uniqueness
        var fullTypeName = classSymbol.ToDisplayString();
        var hash = fullTypeName.GetHashCode();
        return $"PropertyInjectionSource_{Math.Abs(hash):x}";
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
                return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();
            }
        }

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }

        var displayString = typeSymbol.ToDisplayString();

        if (displayString.EndsWith("?"))
        {
            displayString = displayString.TrimEnd('?');
        }

        return displayString;
    }
}

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
