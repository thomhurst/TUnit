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
public sealed class PropertyInjectionSourceGeneratorV2 : IIncrementalGenerator
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

    private static PropertyDataSourceModel ExtractPropertyModel(IPropertySymbol property, AttributeData attribute)
    {
        var propertyType = property.Type;
        var isNullableValueType = propertyType is INamedTypeSymbol
        {
            IsGenericType: true,
            ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        };

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

        return new PropertyDataSourceModel
        {
            PropertyName = property.Name,
            PropertyTypeFullyQualified = GetNonNullableTypeName(propertyType),
            PropertyTypeForTypeof = GetNonNullableTypeString(propertyType),
            ContainingTypeFullyQualified = property.ContainingType.ToDisplayString(),
            IsInitOnly = property.SetMethod?.IsInitOnly == true,
            IsStatic = property.IsStatic,
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

        // Generate UnsafeAccessor methods for init-only properties
        foreach (var prop in model.Properties)
        {
            if (prop.IsInitOnly)
            {
                var backingFieldName = $"<{prop.PropertyName}>k__BackingField";
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
            GeneratePropertyMetadata(sb, prop, model.ClassFullyQualifiedName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(fileName, sb.ToString());
    }

    private static void GeneratePropertyMetadata(StringBuilder sb, PropertyDataSourceModel prop, string classTypeName)
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

    #endregion
}
