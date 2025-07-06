using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class MethodDataSourceExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var methodDataSourceAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
            .ToList();

        foreach (var attr in methodDataSourceAttributes)
        {
            variantIndex = GenerateMethodDataSourceVariants(writer, testInfo, attr, variantIndex);
        }

        return variantIndex;
    }

    private int GenerateMethodDataSourceVariants(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData methodDataSourceAttribute, int variantIndex)
    {
        // For MethodDataSource, we always generate runtime-expandable tests
        // The runtime will handle calling the method and expanding the data
        GenerateRuntimeExpandableTest(writer, testInfo, methodDataSourceAttribute, variantIndex++);
        return variantIndex;
    }

    private void GenerateRuntimeExpandableTest(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData methodDataSourceAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var dataSourceInfo = ExtractDataSourceInfo(methodDataSourceAttribute);
        
        writer.AppendLine($"// Runtime-expandable test for MethodDataSource: {dataSourceInfo.MethodName}");
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataForRuntimeExpansion(writer, testInfo, variantIndex, dataSourceInfo);
        GenerateTestAttributes(writer, testInfo);
        
        // Generate the actual TestDataSource that will be used at runtime
        GenerateMethodDataSource(writer, testInfo, dataSourceInfo);
        
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesForRuntimeExpansion(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateMethodDataSource(CodeWriter writer, TestMethodMetadata testInfo, DataSourceInfo dataSourceInfo)
    {
        writer.AppendLine("DataSources = new TestDataSource[]");
        writer.AppendLine("{");
        writer.Indent();
        
        // Determine the source type (class containing the method)
        var sourceType = dataSourceInfo.ClassType ?? testInfo.TypeSymbol;
        var sourceTypeName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Find the data source method
        var dataSourceMethod = FindDataSourceMethod(sourceType, dataSourceInfo.MethodName);
        if (dataSourceMethod == null)
        {
            writer.AppendLine($"// ERROR: Could not find method {dataSourceInfo.MethodName}");
            writer.Unindent();
            writer.AppendLine("},");
            return;
        }

        // Analyze the method return type to determine the appropriate data source
        var returnType = dataSourceMethod.ReturnType;
        GenerateDataSourceForReturnType(writer, sourceTypeName, dataSourceInfo, dataSourceMethod, returnType);
        
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateDataSourceForReturnType(CodeWriter writer, string sourceTypeName, 
        DataSourceInfo dataSourceInfo, IMethodSymbol method, ITypeSymbol returnType)
    {
        var methodName = dataSourceInfo.MethodName;
        var methodCall = GenerateMethodCall(sourceTypeName, methodName, dataSourceInfo.Arguments);
        
        // Handle Task<T> and ValueTask<T>
        if (IsTaskType(returnType, out var taskResultType))
        {
            GenerateTaskDataSource(writer, methodCall, taskResultType);
            return;
        }
        
        // Handle IAsyncEnumerable<T>
        if (IsAsyncEnumerableType(returnType, out var asyncElementType))
        {
            GenerateAsyncEnumerableDataSource(writer, methodCall, asyncElementType);
            return;
        }
        
        // Handle regular IEnumerable<T>
        if (IsEnumerableType(returnType, out var elementType))
        {
            GenerateSyncEnumerableDataSource(writer, methodCall, elementType);
            return;
        }
        
        // Unsupported return type
        writer.AppendLine($"// ERROR: Unsupported return type {returnType} for method {methodName}");
    }

    private void GenerateSyncEnumerableDataSource(CodeWriter writer, string methodCall, ITypeSymbol elementType)
    {
        writer.AppendLine("new global::TUnit.Core.DelegateDataSource(() =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var enumerable = {methodCall};");
        writer.AppendLine("var result = new global::System.Collections.Generic.List<object?[]>();");
        writer.AppendLine("foreach (var item in enumerable)");
        writer.AppendLine("{");
        writer.Indent();
        
        GenerateDataUnwrapping(writer, "item", elementType);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("return result;");
        
        writer.Unindent();
        writer.AppendLine("})");
    }

    private void GenerateTaskDataSource(CodeWriter writer, string methodCall, ITypeSymbol taskResultType)
    {
        writer.AppendLine("new global::TUnit.Core.TaskDelegateDataSource(async () =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var taskResult = await {methodCall};");
        
        // Check if the task result is an enumerable
        if (IsEnumerableType(taskResultType, out var elementType))
        {
            writer.AppendLine("var result = new global::System.Collections.Generic.List<object?[]>();");
            writer.AppendLine("foreach (var item in taskResult)");
            writer.AppendLine("{");
            writer.Indent();
            
            GenerateDataUnwrapping(writer, "item", elementType);
            
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("return result;");
        }
        else
        {
            writer.AppendLine($"// ERROR: Task result type {taskResultType} is not an enumerable");
            writer.AppendLine("return Array.Empty<object?[]>();");
        }
        
        writer.Unindent();
        writer.AppendLine("})");
    }

    private void GenerateAsyncEnumerableDataSource(CodeWriter writer, string methodCall, ITypeSymbol elementType)
    {
        writer.AppendLine("new global::TUnit.Core.AsyncDelegateDataSource(async (cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var asyncEnumerable = {methodCall};");
        writer.AppendLine("await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))");
        writer.AppendLine("{");
        writer.Indent();
        
        GenerateAsyncDataUnwrapping(writer, "item", elementType);
        
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("})");
    }

    private void GenerateDataUnwrapping(CodeWriter writer, string itemVar, ITypeSymbol elementType)
    {
        // Handle object?[] directly
        if (IsObjectArray(elementType))
        {
            writer.AppendLine($"result.Add({itemVar});");
            return;
        }
        
        // Handle tuples - unwrap into object array
        if (IsTupleType(elementType, out var tupleElements))
        {
            writer.AppendLine("result.Add(new object?[]");
            writer.AppendLine("{");
            writer.Indent();
            
            for (int i = 0; i < tupleElements.Count; i++)
            {
                var itemNumber = i + 1;
                writer.AppendLine($"{itemVar}.Item{itemNumber},");
            }
            
            writer.Unindent();
            writer.AppendLine("});");
            return;
        }
        
        // Single value - wrap in array
        writer.AppendLine($"result.Add(new object?[] {{ {itemVar} }});");
    }

    private void GenerateAsyncDataUnwrapping(CodeWriter writer, string itemVar, ITypeSymbol elementType)
    {
        // For async enumerable, we yield return the unwrapped data
        writer.Append("yield return ");
        
        // Handle object?[] directly
        if (IsObjectArray(elementType))
        {
            writer.AppendLine($"{itemVar};");
            return;
        }
        
        // Handle tuples - unwrap into object array
        if (IsTupleType(elementType, out var tupleElements))
        {
            writer.AppendLine("new object?[]");
            writer.AppendLine("{");
            writer.Indent();
            
            for (int i = 0; i < tupleElements.Count; i++)
            {
                var itemNumber = i + 1;
                writer.AppendLine($"{itemVar}.Item{itemNumber},");
            }
            
            writer.Unindent();
            writer.Append("};");
            return;
        }
        
        // Single value - wrap in array
        writer.AppendLine($"new object?[] {{ {itemVar} }};");
    }

    private string GenerateMethodCall(string sourceTypeName, string methodName, object?[]? arguments)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return $"{sourceTypeName}.{methodName}()";
        }
        
        var argList = string.Join(", ", arguments.Select(arg => TypedConstantParser.FormatPrimitive(arg)));
        return $"{sourceTypeName}.{methodName}({argList})";
    }

    private IMethodSymbol? FindDataSourceMethod(ITypeSymbol type, string methodName)
    {
        return type.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public);
    }

    private bool IsTaskType(ITypeSymbol type, out ITypeSymbol taskResultType)
    {
        taskResultType = null!;
        
        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                taskResultType = namedType.TypeArguments[0];
                return true;
            }
            
            if (namedType.Name == "ValueTask" && namedType.TypeArguments.Length == 1)
            {
                taskResultType = namedType.TypeArguments[0];
                return true;
            }
        }
        
        return false;
    }

    private bool IsAsyncEnumerableType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        elementType = null!;
        
        if (type is INamedTypeSymbol namedType)
        {
            var asyncEnumerable = namedType.AllInterfaces
                .FirstOrDefault(i => i.Name == "IAsyncEnumerable" && i.TypeArguments.Length == 1);
            
            if (asyncEnumerable != null)
            {
                elementType = asyncEnumerable.TypeArguments[0];
                return true;
            }
        }
        
        return false;
    }

    private bool IsEnumerableType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        elementType = null!;
        
        if (type is INamedTypeSymbol namedType)
        {
            var enumerable = namedType.AllInterfaces
                .FirstOrDefault(i => i.Name == "IEnumerable" && i.TypeArguments.Length == 1);
            
            if (enumerable != null)
            {
                elementType = enumerable.TypeArguments[0];
                return true;
            }
            
            // Check if it's directly IEnumerable<T>
            if (namedType.Name == "IEnumerable" && namedType.TypeArguments.Length == 1)
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }
        
        return false;
    }

    private bool IsObjectArray(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.SpecialType == SpecialType.System_Object;
        }
        return false;
    }

    private bool IsTupleType(ITypeSymbol type, out IList<IFieldSymbol> tupleElements)
    {
        tupleElements = Array.Empty<IFieldSymbol>();
        
        if (type is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            tupleElements = namedType.TupleElements.IsDefaultOrEmpty 
                ? Array.Empty<IFieldSymbol>() 
                : namedType.TupleElements.ToList();
            return true;
        }
        
        return false;
    }

    private DataSourceInfo ExtractDataSourceInfo(AttributeData methodDataSourceAttribute)
    {
        var args = methodDataSourceAttribute.ConstructorArguments;
        
        if (args.Length == 0)
            throw new InvalidOperationException("MethodDataSource requires method name");

        // Check if first arg is Type (generic version) or string (non-generic)
        ITypeSymbol? classType = null;
        string methodName;
        
        if (args[0].Kind == TypedConstantKind.Type)
        {
            // Generic version: MethodDataSourceAttribute<T>(methodName)
            classType = args[0].Value as ITypeSymbol;
            methodName = args[1].Value?.ToString() ?? throw new InvalidOperationException("Invalid method name");
        }
        else
        {
            // Non-generic version: MethodDataSourceAttribute(methodName) or MethodDataSourceAttribute(type, methodName)
            if (args.Length > 1 && args[0].Kind == TypedConstantKind.Type)
            {
                classType = args[0].Value as ITypeSymbol;
                methodName = args[1].Value?.ToString() ?? throw new InvalidOperationException("Invalid method name");
            }
            else
            {
                methodName = args[0].Value?.ToString() ?? throw new InvalidOperationException("Invalid method name");
            }
        }
        
        // Check for Arguments property
        object?[]? arguments = null;
        var namedArgs = methodDataSourceAttribute.NamedArguments;
        foreach (var namedArg in namedArgs)
        {
            if (namedArg.Key == "Arguments" && namedArg.Value.Kind == TypedConstantKind.Array)
            {
                arguments = namedArg.Value.Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
                break;
            }
        }

        return new DataSourceInfo(methodName, classType, arguments);
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
            TypedConstantKind.Type => typedConstant.Value,
            TypedConstantKind.Array => typedConstant.Values.Select(ExtractTypedConstantValue).ToArray(),
            _ => typedConstant.Value
        };
    }

    private void GenerateBasicMetadataForRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, DataSourceInfo dataSourceInfo)
    {
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}[{dataSourceInfo.MethodName}]\",");
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
        writer.AppendLine($"// Runtime expansion from MethodDataSource: {dataSourceInfo.MethodName}");
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

    private void GenerateTypedDelegatesForRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        writer.AppendLine($"InstanceFactory = args => new {className}(),");
        writer.AppendLine("TestInvoker = null, // Will be set by TestBuilder for runtime expansion");
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        writer.AppendLine("InvokeTypedTest = null, // Will be set by TestBuilder for runtime expansion");
        writer.AppendLine("CreateExecutableTest = null, // Will be set by TestBuilder for runtime expansion");
    }

    private record DataSourceInfo(string MethodName, ITypeSymbol? ClassType, object?[]? Arguments);
}