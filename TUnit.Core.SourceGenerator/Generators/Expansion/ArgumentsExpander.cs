using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class ArgumentsExpander : ITestExpander
{
    private readonly ITypedConstantFormatter _formatter = new TypedConstantFormatter();
    
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
        // Keep the TypedConstant for proper formatting later
        return typedConstant;
    }

    private void GenerateBasicMetadataWithVariant(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex, object?[] argumentValues)
    {
        var testIdArgs = string.Join(", ", argumentValues.Select(v => 
            v is TypedConstant tc ? _formatter.FormatForTestId(tc) : v?.ToString() ?? "null"));
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
        writer.AppendLine($"// Embedded argument values: {string.Join(", ", argumentValues.Select(v => _formatter.FormatValue(v)))}");
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

        // Generate instance factory
        InstanceFactoryGenerator.GenerateInstanceFactory(writer, testInfo.TypeSymbol);

        // Check if method has CancellationToken parameter
        var hasCancellationToken = testInfo.MethodSymbol.Parameters.Any(p => 
            p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.CancellationToken");
        
        if (hasCancellationToken)
        {
            // TestInvoker doesn't support CancellationToken, so set it to null
            // The test will be executed via InvokeTypedTest which does support it
            writer.AppendLine("TestInvoker = null, // Method has CancellationToken - use InvokeTypedTest instead");
        }
        else
        {
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
        }

        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");

        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var argListWithCancellation = GenerateArgumentListWithCancellation(testInfo, argumentValues);
        if (isAsync)
        {
            writer.AppendLine($"await instance.{methodName}({argListWithCancellation});");
        }
        else
        {
            writer.AppendLine($"instance.{methodName}({argListWithCancellation});");
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
        
        var argsArray = string.Join(", ", argumentValues.Select(v => _formatter.FormatValue(v)));
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

    private string GenerateArgumentList(TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var parameters = testInfo.MethodSymbol.Parameters;
        var formattedArgs = new List<string>();
        var argIndex = 0;

        for (int paramIndex = 0; paramIndex < parameters.Length && argIndex < argumentValues.Length; paramIndex++)
        {
            var paramType = parameters[paramIndex].Type;
            
            // Check if this is the last parameter and it can consume remaining arguments
            if (paramIndex == parameters.Length - 1 && IsEnumerableType(paramType) && 
                argumentValues.Length > parameters.Length)
            {
                // Collect all remaining arguments into an array/enumerable
                var remainingArgs = new List<string>();
                while (argIndex < argumentValues.Length)
                {
                    var value = argumentValues[argIndex];
                    var elementType = GetEnumerableElementType(paramType);
                    var formattedValue = FormatArgumentWithConversion(value, elementType, testInfo);
                    remainingArgs.Add(formattedValue);
                    argIndex++;
                }
                
                // Format as array initializer
                var arrayType = GetEnumerableElementType(paramType)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
                formattedArgs.Add($"new {arrayType}[] {{ {string.Join(", ", remainingArgs)} }}");
            }
            else
            {
                // Regular single argument
                var value = argumentValues[argIndex];
                var formattedValue = FormatArgumentWithConversion(value, paramType, testInfo);
                formattedArgs.Add(formattedValue);
                argIndex++;
            }
        }

        return string.Join(", ", formattedArgs);
    }

    private string GenerateArgumentListWithCancellation(TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var parameters = testInfo.MethodSymbol.Parameters;
        var formattedArgs = new List<string>();
        var argIndex = 0;

        for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
        {
            var parameter = parameters[paramIndex];
            var paramType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            if (paramType == "global::System.Threading.CancellationToken")
            {
                formattedArgs.Add("cancellationToken");
            }
            else if (argIndex < argumentValues.Length)
            {
                // Check if this is the last non-cancellation parameter and it can consume remaining arguments
                var isLastNonCancellationParam = paramIndex == parameters.Length - 1 ||
                    (paramIndex == parameters.Length - 2 && 
                     parameters[parameters.Length - 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.CancellationToken");
                
                if (isLastNonCancellationParam && IsEnumerableType(parameter.Type) && 
                    argumentValues.Length - argIndex > 1)
                {
                    // Collect all remaining arguments into an array/enumerable
                    var remainingArgs = new List<string>();
                    while (argIndex < argumentValues.Length)
                    {
                        var value = argumentValues[argIndex];
                        var elementType = GetEnumerableElementType(parameter.Type);
                        var formattedValue = FormatArgumentWithConversion(value, elementType, testInfo);
                        remainingArgs.Add(formattedValue);
                        argIndex++;
                    }
                    
                    // Format as array initializer
                    var arrayType = GetEnumerableElementType(parameter.Type)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
                    formattedArgs.Add($"new {arrayType}[] {{ {string.Join(", ", remainingArgs)} }}");
                }
                else
                {
                    // Regular single argument
                    var value = argumentValues[argIndex];
                    var formattedValue = FormatArgumentWithConversion(value, parameter.Type, testInfo);
                    formattedArgs.Add(formattedValue);
                    argIndex++;
                }
            }
        }

        return string.Join(", ", formattedArgs);
    }

    private string FormatArgumentWithConversion(object? value, ITypeSymbol? parameterType, TestMethodMetadata testInfo)
    {
        // Handle null values first
        if (value == null)
        {
            return "null";
        }
        
        // First get the basic formatted value
        var formattedValue = _formatter.FormatValue(value, parameterType);
        
        // If both value and parameterType are available, check for conversions
        if (value is TypedConstant typedConstant && parameterType != null && 
            !typedConstant.IsNull && typedConstant.Kind == TypedConstantKind.Primitive && 
            typedConstant.Type != null)
        {
            // Get the source type
            var sourceType = typedConstant.Type;
            
            // Check if types are different and require conversion
            if (!SymbolEqualityComparer.Default.Equals(sourceType, parameterType))
            {
                // Check if the parameter type has user-defined conversion operators
                var conversions = parameterType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.MethodKind == MethodKind.Conversion)
                    .ToList();
                
                // Look for explicit or implicit conversion from source type
                var hasExplicitConversion = conversions.Any(m => 
                    m.Name == "op_Explicit" && 
                    m.Parameters.Length == 1 && 
                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, sourceType));
                
                if (hasExplicitConversion)
                {
                    // Generate explicit cast
                    return $"({parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){formattedValue}";
                }
            }
        }
        
        return formattedValue;
    }

    private bool IsEnumerableType(ITypeSymbol type)
    {
        // Check if it's an array
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        // Check if it implements IEnumerable<T> or IEnumerable
        var enumerableType = type.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" ||
            i.ToDisplayString() == "System.Collections.IEnumerable");
        
        return enumerableType != null;
    }

    private ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
    {
        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        // Handle IEnumerable<T>
        var enumerableInterface = type.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");
        
        if (enumerableInterface != null && enumerableInterface.TypeArguments.Length > 0)
        {
            return enumerableInterface.TypeArguments[0];
        }

        // Check if the type itself is IEnumerable<T>
        if (type is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
            namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }

        // Default to object for non-generic enumerables
        return null;
    }
}