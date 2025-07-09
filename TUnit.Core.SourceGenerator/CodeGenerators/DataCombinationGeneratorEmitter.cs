using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Responsible for emitting the unified DataCombinationGenerator delegate code
/// that handles all data source expansion at compile-time
/// </summary>
public static class DataCombinationGeneratorEmitter
{
    public static void EmitDataCombinationGenerator(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        string methodGuid)
    {
        writer.AppendLine($"private async IAsyncEnumerable<TestDataCombination> GenerateCombinations_{methodGuid}()");
        writer.AppendLine("{");
        writer.Indent();

        var methodDataSources = GetDataSourceAttributes(methodSymbol);
        var classDataSources = GetDataSourceAttributes(typeSymbol);
        var propertyDataSources = GetPropertyDataSources(typeSymbol);

        if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
        {
            writer.AppendLine("yield return new TestDataCombination();");
        }
        else
        {
            var hasRuntimeGenerators = HasRuntimeGenerators(methodDataSources, classDataSources, propertyDataSources);
            
            if (hasRuntimeGenerators)
            {
                writer.AppendLine("// Runtime data source generators detected");
                writer.AppendLine("yield return new TestDataCombination { IsRuntimeGenerated = true };");
            }
            else
            {
                EmitCompileTimeCombinations(writer, methodSymbol, typeSymbol, methodDataSources, classDataSources, propertyDataSources);
            }
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitCompileTimeCombinations(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        ImmutableArray<AttributeData> methodDataSources,
        ImmutableArray<AttributeData> classDataSources,
        ImmutableArray<PropertyWithDataSource> propertyDataSources)
    {
        writer.AppendLine("// Generate all data combinations at compile time");
        writer.AppendLine("var allCombinations = new List<TestDataCombination>();");
        writer.AppendLine("var errorCombination = (TestDataCombination?)null;");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("var methodCombinations = new List<TestDataCombination>();");
        writer.AppendLine("var classCombinations = new List<TestDataCombination>();");
        writer.AppendLine("var propertyCombinations = new List<TestDataCombination>();");
        writer.AppendLine();

        EmitMethodDataCombinations(writer, methodDataSources);
        EmitClassDataCombinations(writer, classDataSources);
        EmitPropertyDataCombinations(writer, propertyDataSources, typeSymbol);

        writer.AppendLine();
        writer.AppendLine("// Ensure we have at least one combination of each type");
        writer.AppendLine("if (methodCombinations.Count == 0) methodCombinations.Add(new TestDataCombination());");
        writer.AppendLine("if (classCombinations.Count == 0) classCombinations.Add(new TestDataCombination());");
        writer.AppendLine("if (propertyCombinations.Count == 0) propertyCombinations.Add(new TestDataCombination());");
        writer.AppendLine();
        
        EmitCartesianProduct(writer);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// If combination generation fails, store error for yielding after try-catch");
        writer.AppendLine("errorCombination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine("DisplayName = $\"[DATA GENERATION ERROR: {ex.Message}]\"");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("// Yield combinations outside of try-catch");
        writer.AppendLine("if (errorCombination != null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("yield return errorCombination;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("else");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("foreach (var combination in allCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("yield return combination;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitMethodDataCombinations(CodeWriter writer, ImmutableArray<AttributeData> methodDataSources)
    {
        writer.AppendLine("// Method data sources");
        for (int i = 0; i < methodDataSources.Length; i++)
        {
            var attr = methodDataSources[i];
            EmitDataSourceCombination(writer, attr, i, "methodCombinations", isClassLevel: false);
        }
    }

    private static void EmitClassDataCombinations(CodeWriter writer, ImmutableArray<AttributeData> classDataSources)
    {
        writer.AppendLine();
        writer.AppendLine("// Class data sources");
        for (int i = 0; i < classDataSources.Length; i++)
        {
            var attr = classDataSources[i];
            EmitDataSourceCombination(writer, attr, i, "classCombinations", isClassLevel: true);
        }
    }

    private static void EmitPropertyDataCombinations(CodeWriter writer, ImmutableArray<PropertyWithDataSource> propertyDataSources, INamedTypeSymbol typeSymbol)
    {
        if (!propertyDataSources.Any())
            return;

        writer.AppendLine();
        writer.AppendLine("// Property data sources");
        writer.AppendLine("var propertyValues = new Dictionary<string, object?>();");
        
        foreach (var propData in propertyDataSources)
        {
            EmitPropertyDataSource(writer, propData, typeSymbol);
        }
        
        writer.AppendLine("propertyCombinations.Add(new TestDataCombination { PropertyValues = propertyValues });");
    }

    private static void EmitDataSourceCombination(CodeWriter writer, AttributeData attr, int index, string listName, bool isClassLevel)
    {
        var attributeClassName = attr.AttributeClass?.Name;

        if (attributeClassName == "ArgumentsAttribute")
        {
            EmitArgumentsAttribute(writer, attr, index, listName, isClassLevel);
        }
        else if (attributeClassName == "MethodDataSourceAttribute")
        {
            EmitMethodDataSource(writer, attr, index, listName, isClassLevel);
        }
        else if (attributeClassName != null && attributeClassName.EndsWith("DataSourceGeneratorAttribute"))
        {
            // Handle any data source generator attribute at compile time
            EmitDataSourceGeneratorAttribute(writer, attr, index, listName, isClassLevel);
        }
        else
        {
            writer.AppendLine($"// Unsupported data source: {attributeClassName}");
            EmitEmptyCombination(writer, index, listName, isClassLevel);
        }
    }

    private static void EmitArgumentsAttribute(CodeWriter writer, AttributeData attr, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine($"// ArgumentsAttribute {index}");
        
        try
        {
            if (attr.ConstructorArguments.Length == 0)
            {
                EmitEmptyCombination(writer, index, listName, isClassLevel);
                return;
            }

            var formattedArgs = new List<string>();
            
            if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                foreach (var value in attr.ConstructorArguments[0].Values)
                {
                    formattedArgs.Add(FormatConstantValue(value));
                }
            }
            else
            {
                formattedArgs = attr.ConstructorArguments.Select(FormatConstantValue).ToList();
            }

            writer.AppendLine($"{listName}.Add(new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();
            
            if (isClassLevel)
            {
                writer.AppendLine($"ClassData = new object?[] {{ {string.Join(", ", formattedArgs)} }},");
                writer.AppendLine($"ClassDataSourceIndex = {index},");
            }
            else
            {
                writer.AppendLine($"MethodData = new object?[] {{ {string.Join(", ", formattedArgs)} }},");
                writer.AppendLine($"MethodDataSourceIndex = {index},");
            }
            
            writer.AppendLine("PropertyValues = new Dictionary<string, object?>()");
            writer.Unindent();
            writer.AppendLine("});");
        }
        catch
        {
            writer.AppendLine($"// Error processing ArgumentsAttribute at index {index}");
            EmitEmptyCombination(writer, index, listName, isClassLevel);
        }
    }

    private static void EmitMethodDataSource(CodeWriter writer, AttributeData attr, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine($"// MethodDataSourceAttribute {index}");
        
        if (attr.ConstructorArguments.Length < 1)
        {
            EmitEmptyCombination(writer, index, listName, isClassLevel);
            return;
        }

        var methodName = attr.ConstructorArguments[0].Value?.ToString();
        if (string.IsNullOrEmpty(methodName))
        {
            EmitEmptyCombination(writer, index, listName, isClassLevel);
            return;
        }

        var isStatic = attr.ConstructorArguments.Length > 1 && 
                      attr.ConstructorArguments[1].Value is bool b && b;

        writer.AppendLine($"// Calling method: {methodName} (static: {isStatic})");
        
        if (isStatic)
        {
            EmitStaticMethodDataSource(writer, methodName, index, listName, isClassLevel);
        }
        else
        {
            EmitInstanceMethodDataSource(writer, methodName, index, listName, isClassLevel);
        }
    }

    private static void EmitStaticMethodDataSource(CodeWriter writer, string methodName, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine($"var dataEnumerable = {methodName}();");
        writer.AppendLine("int loopIndex = 0;");
        writer.AppendLine("await foreach (var data in dataEnumerable)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        if (isClassLevel)
        {
            writer.AppendLine("ClassData = data,");
            writer.AppendLine($"ClassDataSourceIndex = {index},");
            writer.AppendLine("ClassLoopIndex = loopIndex++,");
        }
        else
        {
            writer.AppendLine("MethodData = data,");
            writer.AppendLine($"MethodDataSourceIndex = {index},");
            writer.AppendLine("MethodLoopIndex = loopIndex++,");
        }
        
        writer.AppendLine("PropertyValues = new Dictionary<string, object?>()");
        writer.Unindent();
        writer.AppendLine("});");
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitInstanceMethodDataSource(CodeWriter writer, string methodName, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine("// Instance method - create factory for runtime invocation");
        writer.AppendLine("// For now, marking as runtime-generated since instance methods require runtime handling");
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("IsRuntimeGenerated = true,");
        
        if (isClassLevel)
        {
            writer.AppendLine($"ClassDataSourceIndex = {index},");
        }
        else
        {
            writer.AppendLine($"MethodDataSourceIndex = {index},");
        }
        
        writer.AppendLine("PropertyValues = new Dictionary<string, object?>()");
        writer.Unindent();
        writer.AppendLine("});");
    }

    private static void EmitPropertyDataSource(CodeWriter writer, PropertyWithDataSource propData, INamedTypeSymbol typeSymbol)
    {
        try
        {
            var propertyName = propData.Property.Name;
            var attr = propData.DataSourceAttribute;
            
            if (attr.AttributeClass?.Name == "ArgumentsAttribute")
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Array &&
                        attr.ConstructorArguments[0].Values.Length > 0)
                    {
                        var value = FormatConstantValue(attr.ConstructorArguments[0].Values[0]);
                        writer.AppendLine($"propertyValues[\"{propertyName}\"] = {value};");
                    }
                    else if (attr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
                    {
                        var value = FormatConstantValue(attr.ConstructorArguments[0]);
                        writer.AppendLine($"propertyValues[\"{propertyName}\"] = {value};");
                    }
                }
            }
        }
        catch
        {
            // Ignore errors in property data source processing
        }
    }

    private static void EmitDataSourceGeneratorAttribute(CodeWriter writer, AttributeData attr, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine($"// DataSourceGeneratorAttribute {index}");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        // Create an instance of the generator and call GenerateDataSources
        var attributeType = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
        writer.AppendLine($"var generator = new {attributeType}();");
        writer.AppendLine("var dataGeneratorMetadata = new DataGeneratorMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Type = DataGeneratorType.TestParameters,");
        writer.AppendLine("TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext())");
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.AppendLine("foreach (var dataSourceFunc in generator.GenerateDataSources(dataGeneratorMetadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var data = dataSourceFunc();");
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        if (isClassLevel)
        {
            writer.AppendLine("ClassData = new[] { data },");
            writer.AppendLine($"ClassDataSourceIndex = {index},");
            writer.AppendLine($"ClassLoopIndex = {listName}.Count");
        }
        else
        {
            writer.AppendLine("MethodData = new[] { data },");
            writer.AppendLine($"MethodDataSourceIndex = {index},");
            writer.AppendLine($"MethodLoopIndex = {listName}.Count");
        }
        
        writer.Unindent();
        writer.AppendLine("});");
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("errorCombination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine($"DisplayName = \"Data generation error: \" + ex.Message");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitEmptyCombination(CodeWriter writer, int index, string listName, bool isClassLevel)
    {
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        if (isClassLevel)
        {
            writer.AppendLine($"ClassDataSourceIndex = {index},");
        }
        else
        {
            writer.AppendLine($"MethodDataSourceIndex = {index},");
        }
        
        writer.AppendLine("PropertyValues = new Dictionary<string, object?>()");
        writer.Unindent();
        writer.AppendLine("});");
    }

    private static void EmitCartesianProduct(CodeWriter writer)
    {
        writer.AppendLine("// Generate cartesian product of all combinations");
        writer.AppendLine("foreach (var classCombination in classCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("foreach (var methodCombination in methodCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("foreach (var propertyCombination in propertyCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("var mergedProperties = new Dictionary<string, object?>();");
        writer.AppendLine("foreach (var kvp in classCombination.PropertyValues) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine("foreach (var kvp in methodCombination.PropertyValues) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine("foreach (var kvp in propertyCombination.PropertyValues) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine();
        
        writer.AppendLine("allCombinations.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("ClassData = classCombination.ClassData ?? Array.Empty<object?>(),");
        writer.AppendLine("MethodData = methodCombination.MethodData ?? Array.Empty<object?>(),");
        writer.AppendLine("ClassDataSourceIndex = classCombination.ClassDataSourceIndex,");
        writer.AppendLine("ClassLoopIndex = classCombination.ClassLoopIndex,");
        writer.AppendLine("MethodDataSourceIndex = methodCombination.MethodDataSourceIndex,");
        writer.AppendLine("MethodLoopIndex = methodCombination.MethodLoopIndex,");
        writer.AppendLine("PropertyValues = mergedProperties,");
        writer.AppendLine("DataGenerationException = classCombination.DataGenerationException ?? methodCombination.DataGenerationException ?? propertyCombination.DataGenerationException,");
        writer.AppendLine("DisplayName = classCombination.DisplayName ?? methodCombination.DisplayName ?? propertyCombination.DisplayName");
        writer.Unindent();
        writer.AppendLine("});");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static ImmutableArray<AttributeData> GetDataSourceAttributes(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(a => IsDataSourceAttribute(a.AttributeClass))
            .ToImmutableArray();
    }

    private static ImmutableArray<PropertyWithDataSource> GetPropertyDataSources(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<PropertyWithDataSource>();
        
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                var dataSourceAttr = property.GetAttributes()
                    .FirstOrDefault(a => IsDataSourceAttribute(a.AttributeClass));
                    
                if (dataSourceAttr != null)
                {
                    properties.Add(new PropertyWithDataSource
                    {
                        Property = property,
                        DataSourceAttribute = dataSourceAttr
                    });
                }
            }
        }
        
        return properties.ToImmutableArray();
    }

    private static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;
        
        var name = attributeClass.Name;
        return name == "ArgumentsAttribute" ||
               name == "MethodDataSourceAttribute" ||
               name == "AsyncDataSourceGeneratorAttribute" ||
               name == "AsyncUntypedDataSourceGeneratorAttribute" ||
               name == "NonTypedDataSourceGeneratorAttribute";
    }

    private static bool HasRuntimeGenerators(
        ImmutableArray<AttributeData> methodDataSources,
        ImmutableArray<AttributeData> classDataSources,
        ImmutableArray<PropertyWithDataSource> propertyDataSources)
    {
        return methodDataSources.Any(a => IsRuntimeGenerator(a.AttributeClass)) ||
               classDataSources.Any(a => IsRuntimeGenerator(a.AttributeClass)) ||
               propertyDataSources.Any(p => IsRuntimeGenerator(p.DataSourceAttribute.AttributeClass));
    }

    private static bool IsRuntimeGenerator(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;
        
        var name = attributeClass.Name;
        // Only AsyncUntypedDataSourceGeneratorAttribute is truly runtime-only
        // AsyncDataSourceGeneratorAttribute and DataSourceGeneratorAttribute can be handled at compile time
        return name == "AsyncUntypedDataSourceGeneratorAttribute" ||
               name == "NonTypedDataSourceGeneratorAttribute";
    }

    private static string FormatConstantValue(TypedConstant constant)
    {
        try
        {
            if (constant.IsNull || (constant.Kind != TypedConstantKind.Array && constant.Value == null))
            {
                return "null";
            }

            if (constant.Kind == TypedConstantKind.Array)
            {
                var elementType = constant.Type is IArrayTypeSymbol arrayType
                    ? arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : "object";
                var values = constant.Values.Select(FormatConstantValue);
                return $"new {elementType}[] {{ {string.Join(", ", values)} }}";
            }

            if (constant.Kind == TypedConstantKind.Type && constant.Value is ITypeSymbol typeSymbol)
            {
                return $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
            }

            if (constant.Value is string str)
            {
                return $"\"{str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}\"";
            }

            if (constant.Value is char ch)
            {
                return $"'{ch}'";
            }

            if (constant.Value is bool b)
            {
                return b ? "true" : "false";
            }

            if (constant.Value is float f)
            {
                return $"{f}f";
            }

            if (constant.Value is double d)
            {
                return $"{d}d";
            }

            if (constant.Value is decimal dec)
            {
                return $"{dec}m";
            }

            if (constant.Value is long l)
            {
                return $"{l}L";
            }

            if (constant.Value is uint u)
            {
                return $"{u}u";
            }

            if (constant.Value is ulong ul)
            {
                return $"{ul}ul";
            }

            return constant.Value?.ToString() ?? "null";
        }
        catch
        {
            return "null";
        }
    }

    private struct PropertyWithDataSource
    {
        public IPropertySymbol Property { get; init; }
        public AttributeData DataSourceAttribute { get; init; }
    }
}