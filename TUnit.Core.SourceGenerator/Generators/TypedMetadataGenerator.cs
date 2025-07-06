using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Enhanced metadata generator that creates strongly-typed TestMetadata with compile-time expansion
/// </summary>
public sealed class TypedMetadataGenerator
{
    private readonly DataSourceGenerator _dataSourceGenerator;
    private readonly TupleUnwrapperGenerator _tupleUnwrapperGenerator;

    public TypedMetadataGenerator(DataSourceGenerator dataSourceGenerator)
    {
        _dataSourceGenerator = dataSourceGenerator;
        _tupleUnwrapperGenerator = new TupleUnwrapperGenerator();
    }

    /// <summary>
    /// Generates expanded test metadata for all test methods with compile-time data expansion
    /// </summary>
    public void GenerateExpandedTestRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("var successCount = 0;");
        writer.AppendLine("var failedTests = new List<string>();");
        writer.AppendLine();

        foreach (var testInfo in testMethods)
        {
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();

            // Expand tests with [Arguments] at compile-time
            GenerateExpandedTestMetadata(writer, testInfo);

            writer.AppendLine("successCount++;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("catch (Exception ex)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"var testName = \"{testInfo.TypeSymbol.ToDisplayString()}.{testInfo.MethodSymbol.Name}\";");
            writer.AppendLine("failedTests.Add($\"{testName}: {ex.Message}\");");
            writer.AppendLine("Console.Error.WriteLine($\"Failed to register test {testName}: {ex}\");");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
        }

        writer.AppendLine("if (failedTests.Count > 0)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"Failed to register {failedTests.Count} tests:\");");
        writer.AppendLine("foreach (var failure in failedTests)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"  - {failure}\");");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateExpandedTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods without type arguments
        if (testInfo.MethodSymbol.IsGenericMethod &&
            (testInfo.GenericTypeArguments == null || testInfo.GenericTypeArguments.Length == 0))
        {
            writer.AppendLine($"// Skipped generic method {testInfo.MethodSymbol.Name} - no type arguments provided");
            return;
        }

        // Extract all [Arguments] attributes
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        if (argumentsAttributes.Any())
        {
            // Expand each [Arguments] into a separate test metadata
            int variantIndex = 0;
            foreach (var attr in argumentsAttributes)
            {
                GenerateSingleArgumentsVariant(writer, testInfo, attr, variantIndex++);
            }
        }
        else
        {
            // No [Arguments] - generate single test metadata (might have MethodDataSource)
            GenerateSingleTestMetadata(writer, testInfo, null, 0);
        }
    }

    private void GenerateSingleArgumentsVariant(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData argumentsAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Extract the arguments from the attribute
        var argumentValues = ExtractArgumentValues(argumentsAttribute);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        // Generate basic metadata with variant suffix
        GenerateBasicMetadataWithVariant(writer, testInfo, variantIndex, argumentValues);
        GenerateTestAttributes(writer, testInfo);
        
        // No data sources needed - we're using the expanded arguments
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        
        // Class and property data sources still apply
        _dataSourceGenerator.GenerateClassDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GeneratePropertyDataSourceMetadata(writer, testInfo);
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        
        // Generate delegates with embedded argument values
        GenerateTypedDelegatesWithArguments(writer, testInfo, argumentValues);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateSingleTestMetadata(CodeWriter writer, TestMethodMetadata testInfo, 
        object?[]? argumentValues, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataWithVariant(writer, testInfo, variantIndex, argumentValues);
        GenerateTestAttributes(writer, testInfo);
        
        // Generate data sources for methods without [Arguments]
        if (argumentValues == null)
        {
            _dataSourceGenerator.GenerateDataSourceMetadata(writer, testInfo);
        }
        else
        {
            writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        }
        
        _dataSourceGenerator.GenerateClassDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GeneratePropertyDataSourceMetadata(writer, testInfo);
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        
        if (argumentValues != null)
        {
            GenerateTypedDelegatesWithArguments(writer, testInfo, argumentValues);
        }
        else
        {
            GenerateTypedDelegates(writer, testInfo);
        }

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

        // The Arguments attribute uses params object[], so we need to handle it specially
        if (args.Length == 1 && args[0].Kind == TypedConstantKind.Array)
        {
            // This is the params array - extract its contents
            return args[0].Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
        }
        
        // Otherwise extract all arguments normally
        return args.Select(arg => ExtractTypedConstantValue(arg)).ToArray();
    }
    
    private object? ExtractTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.IsNull)
        {
            return null;
        }
        
        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            return typedConstant.Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
        }
        
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            return typedConstant.Value; // ITypeSymbol
        }
        
        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            // Store the TypedConstant itself to preserve enum information
            return typedConstant;
        }
        
        return typedConstant.Value;
    }

    private static object? GetTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            return typedConstant.Values.Select(v => GetTypedConstantValue(v)).ToArray();
        }
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            return typedConstant.Value; // This is an ITypeSymbol
        }
        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            return typedConstant.Value; // Store the enum value itself
        }
        return typedConstant.Value;
    }

    private void GenerateBasicMetadataWithVariant(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, object?[]? argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var testId = $"{className}.{methodName}";

        // Add variant suffix for expanded tests
        if (argumentValues != null && argumentValues.Length > 0)
        {
            var argString = string.Join(",", argumentValues.Select(FormatArgumentValueForTestId));
            testId += $"({argString})";
        }
        else if (variantIndex > 0)
        {
            testId += $"_variant{variantIndex}";
        }

        writer.AppendLine($"TestId = {TypedConstantParser.FormatPrimitive(testId)},");
        writer.AppendLine($"TestName = \"{methodName}\",");
        writer.AppendLine($"TestClassType = typeof({className}),");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // File location if available
        var location = testInfo.MethodSymbol.Locations.FirstOrDefault();
        if (location != null && location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            writer.AppendLine($"FilePath = @\"{lineSpan.Path}\",");
            writer.AppendLine($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
        }
        
        // Embedded argument values for compile-time expansion
        if (argumentValues != null && argumentValues.Length > 0)
        {
            writer.AppendLine($"// Embedded argument values: {string.Join(", ", argumentValues.Select(FormatArgumentValue))}");
        }
    }

    private string FormatArgumentValue(object? value)
    {
        if (value == null) return "null";
        if (value is object?[] array)
        {
            var elements = array.Select(FormatArgumentValue);
            return $"new[] {{ {string.Join(", ", elements)} }}";
        }
        if (value is TypedConstant typedConstant)
        {
            // Handle TypedConstant for enums
            if (typedConstant.Kind == TypedConstantKind.Enum && typedConstant.Type != null)
            {
                var enumTypeName = typedConstant.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Get the enum member name by looking up the field
                var enumValue = typedConstant.Value;
                if (enumValue != null && typedConstant.Type is INamedTypeSymbol enumType)
                {
                    foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (member.HasConstantValue && member.ConstantValue?.Equals(enumValue) == true)
                        {
                            return $"{enumTypeName}.{member.Name}";
                        }
                    }
                }
                // Fallback: use the numeric value cast to the enum type
                return $"({enumTypeName}){enumValue}";
            }
            return FormatArgumentValue(typedConstant.Value);
        }
        if (value is ITypeSymbol typeSymbol)
        {
            return $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
        }
        if (value is IFieldSymbol fieldSymbol && fieldSymbol.HasConstantValue)
        {
            // This is an enum value
            var enumType = fieldSymbol.ContainingType;
            return $"{enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{fieldSymbol.Name}";
        }
        if (value is string s) return TypedConstantParser.FormatPrimitive(s);
        if (value is char c) return TypedConstantParser.FormatPrimitive(c);
        if (value is bool b) return b.ToString().ToLower();
        if (value is float f) return TypedConstantParser.FormatPrimitive(f);
        if (value is double d) return TypedConstantParser.FormatPrimitive(d);
        if (value is decimal dec) return $"{dec}m";
        if (value is long l) return $"{l}L";
        if (value is ulong ul) return $"{ul}UL";
        if (value is uint ui) return $"{ui}U";
        // For primitive values, check if they need special formatting
        if (value is int || value is long || value is short || value is byte ||
            value is uint || value is ulong || value is ushort || value is sbyte)
        {
            // This could be an enum value that lost its type information
            // We'll need to rely on FormatArgumentValueWithCast to add the cast
            return value.ToString() ?? "null";
        }
        // Check if it's an enum by looking at the type
        var type = value?.GetType();
        if (type != null && type.IsEnum)
        {
            return $"{type.FullName}.{value}";
        }
        return value.ToString() ?? "null";
    }

    private string FormatArgumentValueForTestId(object? value)
    {
        if (value == null) return "null";
        if (value is object?[] array)
        {
            var elements = array.Select(FormatArgumentValueForTestId);
            return $"[{string.Join(",", elements)}]";
        }
        if (value is TypedConstant typedConstant)
        {
            if (typedConstant.Kind == TypedConstantKind.Enum)
            {
                // Get the enum member name by looking up the field
                var enumValue = typedConstant.Value;
                if (enumValue != null && typedConstant.Type is INamedTypeSymbol enumType)
                {
                    foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (member.HasConstantValue && member.ConstantValue?.Equals(enumValue) == true)
                        {
                            return member.Name;
                        }
                    }
                }
                // Fallback: use the numeric value
                return enumValue?.ToString() ?? "null";
            }
            return FormatArgumentValueForTestId(typedConstant.Value);
        }
        if (value is ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString();
        }
        if (value is IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.Name;
        }
        return value.ToString() ?? "null";
    }

    private void GenerateTestAttributes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        GenerateCategories(writer, testInfo);
        GenerateSkipStatus(writer, testInfo);
        GenerateTimeout(writer, testInfo);
        GenerateRetryCount(writer, testInfo);
        GenerateParallelization(writer, testInfo);
        GenerateDependencies(writer, testInfo);
        GenerateAttributeTypes(writer, testInfo);
    }

    private void GenerateCategories(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var categories = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "CategoryAttribute")
            .Select(a => GetTypedConstantValue(a.ConstructorArguments.FirstOrDefault())?.ToString())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        if (categories.Any())
        {
            writer.AppendLine($"Categories = new string[] {{ {string.Join(", ", categories.Select(c => $"\"{c}\""))} }},");
        }
        else
        {
            writer.AppendLine("Categories = Array.Empty<string>(),");
        }
    }

    private void GenerateSkipStatus(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var skipAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SkipAttribute");

        if (skipAttribute != null)
        {
            var reason = GetTypedConstantValue(skipAttribute.ConstructorArguments.FirstOrDefault())?.ToString() ?? "No reason provided";
            writer.AppendLine("IsSkipped = true,");
            writer.AppendLine($"SkipReason = \"{reason}\",");
        }
        else
        {
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
        }
    }

    private void GenerateTimeout(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var timeoutAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TimeoutAttribute");

        if (timeoutAttribute != null)
        {
            var timeout = GetTypedConstantValue(timeoutAttribute.ConstructorArguments.FirstOrDefault());
            writer.AppendLine($"TimeoutMs = {timeout},");
        }
        else
        {
            writer.AppendLine("TimeoutMs = null,");
        }
    }

    private void GenerateRetryCount(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var retryAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RetryAttribute");

        if (retryAttribute != null)
        {
            var retryCount = GetTypedConstantValue(retryAttribute.ConstructorArguments.FirstOrDefault()) ?? 0;
            writer.AppendLine($"RetryCount = {retryCount},");
        }
        else
        {
            writer.AppendLine("RetryCount = 0,");
        }
    }

    private void GenerateParallelization(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var notInParallelAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NotInParallelAttribute");

        writer.AppendLine($"CanRunInParallel = {(notInParallelAttribute == null).ToString().ToLower()},");
    }

    private void GenerateDependencies(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var dependsOnAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "DependsOnAttribute" ||
                       a.AttributeClass?.Name.StartsWith("DependsOnAttribute`") == true)
            .ToList();

        var testDependencies = new List<string>();

        foreach (var attr in dependsOnAttributes)
        {
            if (attr.AttributeClass?.IsGenericType == true)
            {
                var typeArg = attr.AttributeClass.TypeArguments.FirstOrDefault();
                if (typeArg != null)
                {
                    var testName = GetTypedConstantValue(attr.ConstructorArguments.FirstOrDefault())?.ToString();
                    if (string.IsNullOrEmpty(testName))
                    {
                        testDependencies.Add($"TestDependency.FromClass(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                    }
                    else
                    {
                        testDependencies.Add($"TestDependency.FromClassAndMethod(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), \"{testName}\")");
                    }
                }
            }
        }

        if (testDependencies.Any())
        {
            writer.AppendLine("Dependencies = new TestDependency[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var dep in testDependencies)
            {
                writer.AppendLine($"{dep},");
            }
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("Dependencies = Array.Empty<TestDependency>(),");
        }
    }

    private void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        IList<IParameterSymbol> parameters;

        if (testInfo.MethodSymbol.IsGenericMethod && testInfo.GenericTypeArguments != null &&
            testInfo.GenericTypeArguments.Length == testInfo.MethodSymbol.TypeParameters.Length)
        {
            try
            {
                var constructedMethod = testInfo.MethodSymbol.Construct(testInfo.GenericTypeArguments);
                parameters = constructedMethod.Parameters;
            }
            catch
            {
                parameters = testInfo.MethodSymbol.Parameters;
            }
        }
        else
        {
            parameters = testInfo.MethodSymbol.Parameters;
        }

        if (!parameters.Any())
        {
            writer.AppendLine("ParameterTypes = Type.EmptyTypes,");
            writer.AppendLine("TestMethodParameterTypes = Array.Empty<string>(),");
            return;
        }

        writer.AppendLine("ParameterTypes = new Type[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in parameters)
        {
            var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"typeof({typeName}),");
        }

        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in parameters)
        {
            var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"\"{typeName}\",");
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

    private void GenerateAttributeTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("AttributeFactory = null,");
    }

    private void GenerateTypedDelegates(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        GenerateInlineInstanceFactory(writer, testInfo);
        GenerateInlineTestInvoker(writer, testInfo);
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        GenerateInlineTypedTestInvoker(writer, testInfo);
        GenerateInlineExecutableTestFactory(writer, testInfo);
    }

    private void GenerateTypedDelegatesWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        GenerateInlineInstanceFactory(writer, testInfo);
        GenerateInlineTestInvokerWithArguments(writer, testInfo, argumentValues);
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        GenerateInlineTypedTestInvokerWithArguments(writer, testInfo, argumentValues);
        GenerateInlineExecutableTestFactoryWithArguments(writer, testInfo, argumentValues);
    }

    private void GenerateInlineInstanceFactory(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var constructorArgs = GenerateConstructorArgs(testInfo);
        writer.AppendLine($"InstanceFactory = args => new {className}({constructorArgs}),");
    }

    private void GenerateInlineTestInvoker(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"var typedInstance = ({className})instance;");

        var parameters = testInfo.MethodSymbol.Parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
        }

        var argList = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(i => $"arg{i}"));
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
    }

    private void GenerateInlineTestInvokerWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"var typedInstance = ({className})instance;");
        
        // Use embedded argument values instead of runtime args
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var parameters = testInfo.MethodSymbol.Parameters;
        var argList = new List<string>();
        
        // Check if we need to unwrap tuples
        var unwrappedArgs = UnwrapArgumentsIfNeeded(argumentValues, parameters);
        
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i < unwrappedArgs.Length)
            {
                var paramType = parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var argValue = FormatArgumentValueWithCast(unwrappedArgs[i], paramType);
                argList.Add(argValue);
            }
        }

        if (isAsync)
        {
            writer.AppendLine($"await typedInstance.{methodName}({string.Join(", ", argList)});");
        }
        else
        {
            writer.AppendLine($"typedInstance.{methodName}({string.Join(", ", argList)});");
            writer.AppendLine("await Task.CompletedTask;");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateInlineTypedTestInvoker(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;
        
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        var parameters = testInfo.MethodSymbol.Parameters;
        var paramIndex = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.Type.ToDisplayString() == "System.Threading.CancellationToken")
            {
                writer.AppendLine($"var arg{i} = cancellationToken;");
            }
            else
            {
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                writer.AppendLine($"var arg{i} = ({paramType})args[{paramIndex}]!;");
                paramIndex++;
            }
        }
        
        var argList = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(i => $"arg{i}"));
        if (isAsync)
        {
            writer.AppendLine($"await instance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"instance.{methodName}({argList});");
        }
        
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateInlineTypedTestInvokerWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;
        
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var parameters = testInfo.MethodSymbol.Parameters;
        var argList = new List<string>();
        
        // Check if we need to unwrap tuples
        var unwrappedArgs = UnwrapArgumentsIfNeeded(argumentValues, parameters);
        var argIndex = 0;
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.Type.ToDisplayString() == "System.Threading.CancellationToken")
            {
                argList.Add("cancellationToken");
            }
            else if (argIndex < unwrappedArgs.Length)
            {
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var argValue = FormatArgumentValueWithCast(unwrappedArgs[argIndex], paramType);
                argList.Add(argValue);
                argIndex++;
            }
        }
        
        if (isAsync)
        {
            writer.AppendLine($"await instance.{methodName}({string.Join(", ", argList)});");
        }
        else
        {
            writer.AppendLine($"instance.{methodName}({string.Join(", ", argList)});");
        }
        
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateInlineExecutableTestFactory(CodeWriter writer, TestMethodMetadata testInfo)
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
        writer.AppendLine("Arguments = creationContext.Arguments,");
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

    private void GenerateInlineExecutableTestFactoryWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
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
        
        // Use embedded arguments instead of creationContext.Arguments
        // Check if we need to unwrap tuples
        var parameters = testInfo.MethodSymbol.Parameters.Where(p => p.Type.ToDisplayString() != "System.Threading.CancellationToken").ToList();
        var unwrappedArgs = UnwrapArgumentsIfNeeded(argumentValues, parameters);
        
        writer.Append("Arguments = new object?[] { ");
        for (int i = 0; i < unwrappedArgs.Length; i++)
        {
            if (i > 0) writer.Append(", ");
            writer.Append(FormatArgumentValue(unwrappedArgs[i]));
        }
        writer.AppendLine(" },");
        
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

    private string FormatArgumentValueWithCast(object? value, string targetType)
    {
        var formattedValue = FormatArgumentValue(value);
        
        // Check if we need to cast a numeric value to an enum
        if (value != null && IsNumericType(value.GetType()) && IsEnumType(targetType))
        {
            // For negative numbers, we need to wrap them in parentheses
            if (formattedValue.StartsWith("-"))
            {
                return $"({targetType})({formattedValue})";
            }
            return $"({targetType}){formattedValue}";
        }
        
        // For value types that need explicit casting
        if (value != null && IsValueType(targetType))
        {
            // For negative numbers, we need to wrap them in parentheses
            if (formattedValue.StartsWith("-"))
            {
                return $"({targetType})({formattedValue})";
            }
            return $"({targetType}){formattedValue}";
        }
        
        return formattedValue;
    }

    private bool IsValueType(string typeName)
    {
        return typeName switch
        {
            "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" => true,
            "float" or "double" or "decimal" => true,
            "bool" or "char" => true,
            _ when typeName.StartsWith("System.Nullable<") => true,
            _ => false
        };
    }
    
    private bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }
    
    private bool IsEnumType(string typeName)
    {
        // Simple heuristic: if it's not a known primitive type and not a generic, 
        // it might be an enum. This is not perfect but works for most cases.
        return !IsValueType(typeName) && 
               !typeName.StartsWith("System.") && 
               !typeName.Contains("<") &&
               !typeName.Contains("[]") &&
               typeName != "string" && 
               typeName != "object";
    }

    private string GenerateConstructorArgs(TestMethodMetadata testInfo)
    {
        var constructor = testInfo.TypeSymbol.Constructors
            .FirstOrDefault(c => !c.IsStatic);

        if (constructor == null || constructor.Parameters.Length == 0)
            return "";

        var args = new List<string>();
        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var paramType = constructor.Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            args.Add($"({paramType})args[{i}]");
        }

        return string.Join(", ", args);
    }

    private object?[] UnwrapArgumentsIfNeeded(object?[] argumentValues, IList<IParameterSymbol> parameters)
    {
        // If we have a single tuple argument and multiple parameters, unwrap it
        if (argumentValues.Length == 1 && parameters.Count > 1)
        {
            var value = argumentValues[0];
            if (value != null && IsTupleValue(value))
            {
                return UnwrapTupleValue(value);
            }
        }
        
        return argumentValues;
    }

    private bool IsTupleValue(object value)
    {
        var type = value.GetType();
        return type.IsGenericType && 
               (type.FullName?.StartsWith("System.Tuple") == true || 
                type.FullName?.StartsWith("System.ValueTuple") == true);
    }

    private object?[] UnwrapTupleValue(object tupleValue)
    {
        var result = new List<object?>();
        var type = tupleValue.GetType();
        
        if (type.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // Handle ValueTuple
            var fields = type.GetFields().OrderBy(f => f.Name).ToList();
            foreach (var field in fields)
            {
                if (field.Name == "Rest")
                {
                    var rest = field.GetValue(tupleValue);
                    if (rest != null && IsTupleValue(rest))
                    {
                        result.AddRange(UnwrapTupleValue(rest));
                    }
                    else
                    {
                        result.Add(rest);
                    }
                }
                else
                {
                    result.Add(field.GetValue(tupleValue));
                }
            }
        }
        else if (type.FullName?.StartsWith("System.Tuple") == true)
        {
            // Handle reference Tuple
            var properties = type.GetProperties()
                .Where(p => p.Name.StartsWith("Item"))
                .OrderBy(p => p.Name)
                .ToList();
                
            foreach (var prop in properties)
            {
                if (prop.Name == "Rest")
                {
                    var rest = prop.GetValue(tupleValue);
                    if (rest != null && IsTupleValue(rest))
                    {
                        result.AddRange(UnwrapTupleValue(rest));
                    }
                    else
                    {
                        result.Add(rest);
                    }
                }
                else
                {
                    result.Add(prop.GetValue(tupleValue));
                }
            }
        }
        
        return result.ToArray();
    }
}