using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class ArgumentsExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ArgumentsAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        foreach (var attr in argumentsAttributes)
        {
            GenerateSingleArgumentsVariant(writer, testInfo, attr, variantIndex++);
        }

        return variantIndex;
    }

    private void GenerateSingleArgumentsVariant(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData argumentsAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var argumentValues = ExtractArgumentValues(argumentsAttribute);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataWithVariant(writer, testInfo, variantIndex, argumentValues);
        GenerateTestAttributes(writer, testInfo);
        
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesWithArguments(writer, testInfo, argumentValues);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private object?[] ExtractArgumentValues(AttributeData argumentsAttribute)
    {
        var args = argumentsAttribute.ConstructorArguments;
        
        if (args.IsDefaultOrEmpty)
        {
            return Array.Empty<object?>();
        }

        if (args.Length == 1 && args[0].Kind == TypedConstantKind.Array)
        {
            return args[0].Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
        }
        
        return args.Select(arg => ExtractTypedConstantValue(arg)).ToArray();
    }

    private object? ExtractTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.IsNull)
        {
            return null;
        }
        
        return typedConstant.Kind switch
        {
            TypedConstantKind.Primitive => typedConstant.Value,
            TypedConstantKind.Enum => typedConstant.Value,
            TypedConstantKind.Type => typedConstant, // Keep the TypedConstant for Type arguments so we can format them as typeof(...)
            TypedConstantKind.Array => typedConstant, // Keep the TypedConstant for arrays so we can format them properly later
            _ => typedConstant.Value
        };
    }

    private void GenerateBasicMetadataWithVariant(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex, object?[] argumentValues)
    {
        var testIdArgs = string.Join(", ", argumentValues.Select(v => FormatArgumentForTestId(v)));
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}({testIdArgs})\",");
        writer.AppendLine($"TestName = \"{testInfo.MethodSymbol.Name}\",");
        writer.AppendLine($"TestClassType = typeof({testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
        writer.AppendLine($"TestMethodName = \"{testInfo.MethodSymbol.Name}\",");
        var location = testInfo.MethodSymbol.Locations.FirstOrDefault();
        if (location != null && location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            writer.AppendLine($"FilePath = @\"{lineSpan.Path?.Replace("\\", "\\\\")}\",");
            writer.AppendLine($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
        }
        else
        {
            writer.AppendLine("FilePath = null,");
            writer.AppendLine("LineNumber = 0,");
        }
        writer.AppendLine($"// Embedded argument values: {string.Join(", ", argumentValues.Select(v => TypedConstantParser.FormatPrimitive(v)))}");
    }

    private void GenerateTestAttributes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("Categories = Array.Empty<string>(),");
        writer.AppendLine("IsSkipped = false,");
        writer.AppendLine("SkipReason = null,");
        writer.AppendLine("TimeoutMs = null,");
        writer.AppendLine("RetryCount = 0,");
        writer.AppendLine("CanRunInParallel = true,");
        writer.AppendLine("Dependencies = Array.Empty<TestDependency>(),");
        writer.AppendLine("AttributeFactory = null,");
    }

    private void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("ParameterTypes = new Type[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"typeof({paramType}),");
        }
        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"\"{paramType}\",");
        }
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateEmptyHookMetadata(CodeWriter writer)
    {
        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("BeforeClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("BeforeTest = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterTest = Array.Empty<HookMetadata>()");
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateTypedDelegatesWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        // Generate instance factory based on whether the class has constructor parameters
        if (HasClassConstructorParameters(testInfo.TypeSymbol))
        {
            // For classes with constructor parameters, leave InstanceFactory null
            // The TestBuilder will handle instance creation with proper constructor arguments
            writer.AppendLine("InstanceFactory = null,");
        }
        else
        {
            // For classes with default constructor, generate the factory
            writer.AppendLine($"InstanceFactory = args => new {className}(),");
        }

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var typedInstance = ({className})instance;");
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var argList = GenerateArgumentList(testInfo, argumentValues);
        if (isAsync)
        {
            writer.AppendLine($"await typedInstance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"typedInstance.{methodName}({argList});");
            writer.AppendLine("await Task.CompletedTask;");
        }
        
        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");

        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var argList2 = GenerateArgumentList(testInfo, argumentValues);
        if (isAsync)
        {
            writer.AppendLine($"await instance.{methodName}({argList2});");
        }
        else
        {
            writer.AppendLine($"instance.{methodName}({argList2});");
        }
        
        writer.Unindent();
        writer.AppendLine("},");

        GenerateExecutableTestFactory(writer, testInfo, argumentValues);
    }

    private void GenerateExecutableTestFactory(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine("CreateExecutableTest = (creationContext, metadata) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"return new global::TUnit.Engine.ExecutableTest<{className}>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("TestId = creationContext.TestId,");
        writer.AppendLine("DisplayName = creationContext.DisplayName,");
        writer.AppendLine("Metadata = metadata,");
        
        var argsArray = string.Join(", ", argumentValues.Select(v => FormatArgumentValue(v)));
        writer.AppendLine($"Arguments = new object?[] {{ {argsArray} }},");
        
        writer.AppendLine("ClassArguments = creationContext.ClassArguments,");
        writer.AppendLine("PropertyValues = creationContext.PropertyValues,");
        writer.AppendLine("BeforeTestHooks = creationContext.BeforeTestHooks,");
        writer.AppendLine("AfterTestHooks = creationContext.AfterTestHooks,");
        writer.AppendLine("Context = creationContext.Context,");
        
        writer.AppendLine("CreateTypedInstance = async () =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (metadata.InstanceFactory != null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return await global::System.Threading.Tasks.Task.FromResult(metadata.InstanceFactory(creationContext.ClassArguments));");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine($"throw new global::System.InvalidOperationException(\"No instance factory for {className}\");");
        writer.Unindent();
        writer.AppendLine("},");
        
        writer.AppendLine("InvokeTypedTest = metadata.InvokeTypedTest ?? throw new global::System.InvalidOperationException(\"No test invoker\")");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("},");
    }

    private string FormatArgumentValue(object? value)
    {
        if (value is TypedConstant tc)
        {
            return TypedConstantParser.GetRawTypedConstantValue(tc);
        }
        return TypedConstantParser.FormatPrimitive(value);
    }

    private string GenerateArgumentList(TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var parameters = testInfo.MethodSymbol.Parameters;
        var formattedArgs = new List<string>();

        for (int i = 0; i < argumentValues.Length && i < parameters.Length; i++)
        {
            var value = argumentValues[i];
            var paramType = parameters[i].Type;
            var formattedValue = FormatArgumentWithType(value, paramType);
            formattedArgs.Add(formattedValue);
        }

        return string.Join(", ", formattedArgs);
    }

    private string FormatArgumentWithType(object? value, ITypeSymbol targetType)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle TypedConstant for arrays and other complex types
        if (value is TypedConstant tc)
        {
            return TypedConstantParser.GetRawTypedConstantValue(tc);
        }

        // Handle enum types
        if (targetType.TypeKind == TypeKind.Enum && targetType is INamedTypeSymbol enumType)
        {
            // Try to find the enum member name for this value
            var enumMemberName = GetEnumMemberName(enumType, value);
            if (enumMemberName != null)
            {
                var enumTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{enumTypeName}.{enumMemberName}";
            }
            
            // Fallback to cast syntax for values without named members (like -1)
            var formattedValue = TypedConstantParser.FormatPrimitive(value);
            var enumTypeNameForCast = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // Handle negative values by wrapping them in parentheses
            if (formattedValue.StartsWith("-"))
            {
                return $"({enumTypeNameForCast})({formattedValue})";
            }
            
            return $"({enumTypeNameForCast}){formattedValue}";
        }

        // Handle float types - all numeric values stored as double need 'f' suffix for float
        if (targetType.SpecialType == SpecialType.System_Single)
        {
            if (value is double doubleValue)
            {
                return $"{doubleValue}f";
            }
            if (value is float floatValue)
            {
                return $"{floatValue}f";
            }
            if (value is int intValue)
            {
                return $"{intValue}f";
            }
            if (value is long longValue)
            {
                return $"{longValue}f";
            }
            // Fallback for any numeric type
            return $"{value}f";
        }

        // Default formatting
        return TypedConstantParser.FormatPrimitive(value);
    }

    private string? GetEnumMemberName(INamedTypeSymbol enumType, object value)
    {
        // Convert the value to the underlying type of the enum
        var underlyingType = enumType.EnumUnderlyingType;
        if (underlyingType == null)
            return null;

        // Get all enum members
        foreach (var member in enumType.GetMembers())
        {
            if (member is IFieldSymbol field && field.IsConst && field.HasConstantValue)
            {
                // Compare the constant values
                if (AreValuesEqual(field.ConstantValue, value))
                {
                    return field.Name;
                }
            }
        }

        return null;
    }

    private bool AreValuesEqual(object? enumValue, object? providedValue)
    {
        if (enumValue == null || providedValue == null)
            return enumValue == providedValue;

        // Convert both to long for comparison (handles most integer types)
        try
        {
            var enumLong = Convert.ToInt64(enumValue);
            var providedLong = Convert.ToInt64(providedValue);
            return enumLong == providedLong;
        }
        catch
        {
            // Fall back to direct comparison
            return enumValue.Equals(providedValue);
        }
    }

    private bool HasClassConstructorParameters(ITypeSymbol typeSymbol)
    {
        // Check if the class has any constructors with parameters
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic);

        // If there's a parameterized constructor, return true
        return constructors.Any(c => c.Parameters.Length > 0);
    }

    private static string FormatArgumentForTestId(object? value)
    {
        if (value == null)
            return "null";
        
        // Handle TypedConstant
        if (value is TypedConstant tc)
        {
            switch (tc.Kind)
            {
                case TypedConstantKind.Array:
                    var elements = tc.Values.Select(v => FormatArgumentForTestId(v.Value));
                    return $"[{string.Join(", ", elements)}]";
                case TypedConstantKind.Type:
                    return ((ITypeSymbol)tc.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                default:
                    value = tc.Value;
                    break;
            }
        }
        
        var str = value?.ToString() ?? "null";
        
        // Escape special characters for TestId
        return str.Replace("\\", "\\\\")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t")
                  .Replace("\"", "\\\"");
    }
}