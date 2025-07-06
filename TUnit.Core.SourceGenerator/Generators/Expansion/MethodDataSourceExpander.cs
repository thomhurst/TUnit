using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class MethodDataSourceExpander : ITestExpander
{
    private readonly ITypedConstantFormatter _formatter = new TypedConstantFormatter();
    
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
        var dataSourceInfo = ExtractDataSourceInfo(methodDataSourceAttribute);
        
        // Determine the source type (class containing the method)
        var sourceType = dataSourceInfo.ClassType ?? testInfo.TypeSymbol;
        var sourceTypeName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Find the data source method
        var dataSourceMethod = FindDataSourceMethod(sourceType, dataSourceInfo.MethodName);
        if (dataSourceMethod == null)
        {
            writer.AppendLine($"// ERROR: Could not find method {dataSourceInfo.MethodName} on type {sourceTypeName}");
            return variantIndex;
        }

        // Generate expansion code based on method return type
        GenerateDataSourceExpansion(writer, testInfo, dataSourceInfo, dataSourceMethod, sourceTypeName, ref variantIndex);
        
        return variantIndex;
    }

    private void GenerateDataSourceExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        DataSourceInfo dataSourceInfo, IMethodSymbol dataSourceMethod, string sourceTypeName, ref int variantIndex)
    {
        var returnType = dataSourceMethod.ReturnType;
        var methodCall = GenerateMethodCall(sourceTypeName, dataSourceInfo.MethodName, dataSourceInfo.Arguments);
        
        // Generate appropriate expansion based on return type
        if (IsTaskType(returnType, out var taskResultType))
        {
            GenerateTaskExpansion(writer, testInfo, methodCall, taskResultType, dataSourceInfo, ref variantIndex);
        }
        else if (IsAsyncEnumerableType(returnType, out var asyncElementType))
        {
            GenerateAsyncEnumerableExpansion(writer, testInfo, methodCall, asyncElementType, dataSourceInfo, ref variantIndex);
        }
        else if (IsEnumerableType(returnType, out var elementType))
        {
            GenerateSyncEnumerableExpansion(writer, testInfo, methodCall, elementType, dataSourceInfo, ref variantIndex);
        }
        else
        {
            writer.AppendLine($"// ERROR: Unsupported return type {returnType} for method {dataSourceInfo.MethodName}");
        }
    }

    private void GenerateSyncEnumerableExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        string methodCall, ITypeSymbol elementType, DataSourceInfo dataSourceInfo, ref int variantIndex)
    {
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var dataSource = {methodCall};");
        writer.AppendLine("foreach (var dataItem in dataSource)");
        writer.AppendLine("{");
        writer.Indent();
        
        GenerateTestMetadataForDataItem(writer, testInfo, "dataItem", elementType, dataSourceInfo, variantIndex++);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateTaskExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        string methodCall, ITypeSymbol taskResultType, DataSourceInfo dataSourceInfo, ref int variantIndex)
    {
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var dataSourceTask = {methodCall};");
        writer.AppendLine($"var dataSource = await dataSourceTask;");
        
        if (IsEnumerableType(taskResultType, out var elementType))
        {
            writer.AppendLine("foreach (var dataItem in dataSource)");
            writer.AppendLine("{");
            writer.Indent();
            
            GenerateTestMetadataForDataItem(writer, testInfo, "dataItem", elementType, dataSourceInfo, variantIndex++);
            
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            writer.AppendLine($"// ERROR: Task result type {taskResultType} is not an enumerable");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateAsyncEnumerableExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        string methodCall, ITypeSymbol elementType, DataSourceInfo dataSourceInfo, ref int variantIndex)
    {
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var asyncDataSource = {methodCall};");
        writer.AppendLine("await foreach (var dataItem in asyncDataSource)");
        writer.AppendLine("{");
        writer.Indent();
        
        GenerateTestMetadataForDataItem(writer, testInfo, "dataItem", elementType, dataSourceInfo, variantIndex++);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateTestMetadataForDataItem(CodeWriter writer, TestMethodMetadata testInfo, 
        string dataItemVar, ITypeSymbol elementType, DataSourceInfo dataSourceInfo, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Generate code to unwrap the data item into an object array
        GenerateDataUnwrapping(writer, dataItemVar, elementType);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();
        
        GenerateMetadataWithArguments(writer, testInfo, variantIndex, dataSourceInfo);
        GenerateTestAttributes(writer, testInfo);
        
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesWithArguments(writer, testInfo);
        
        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateDataUnwrapping(CodeWriter writer, string dataItemVar, ITypeSymbol elementType)
    {
        writer.Append("var arguments = ");
        
        // Handle object?[] directly
        if (IsObjectArray(elementType))
        {
            writer.AppendLine($"{dataItemVar};");
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
                writer.AppendLine($"{dataItemVar}.Item{itemNumber},");
            }
            
            writer.Unindent();
            writer.AppendLine("};");
            return;
        }
        
        // Single value - wrap in array
        writer.AppendLine($"new object?[] {{ {dataItemVar} }};");
    }

    private void GenerateMetadataWithArguments(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, DataSourceInfo dataSourceInfo)
    {
        // Generate test ID with argument placeholders
        writer.AppendLine($"TestId = $\"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}({{string.Join(\", \", arguments.Select(a => a?.ToString()?.Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"\\r\", \"\\\\r\").Replace(\"\\n\", \"\\\\n\").Replace(\"\\t\", \"\\\\t\").Replace(\"\\\"\", \"\\\\\\\"\") ?? \"null\"))}})\",");
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
        writer.AppendLine($"// Expanded from MethodDataSource: {dataSourceInfo.MethodName}");
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

    private void GenerateTypedDelegatesWithArguments(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        // Generate instance factory based on whether the class has constructor parameters
        if (testInfo.TypeSymbol.HasParameterizedConstructor())
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

        // Generate test invoker that uses the captured arguments
        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var typedInstance = ({className})instance;");
        
        // Generate method call with proper argument casting
        var parameterCasts = new List<string>();
        for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
        {
            var param = testInfo.MethodSymbol.Parameters[i];
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            parameterCasts.Add($"({paramType})arguments[{i}]");
        }
        
        var argList = string.Join(", ", parameterCasts);
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

        // Generate strongly-typed test invoker
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        
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

        GenerateExecutableTestFactory(writer, testInfo);
    }

    private void GenerateExecutableTestFactory(CodeWriter writer, TestMethodMetadata testInfo)
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
        writer.AppendLine("Arguments = arguments,");
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

    private string GenerateMethodCall(string sourceTypeName, string methodName, object?[]? arguments)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return $"{sourceTypeName}.{methodName}()";
        }
        
        var argList = string.Join(", ", arguments.Select(arg => _formatter.FormatValue(arg)));
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
            
            // Check if it's directly IAsyncEnumerable<T>
            if (namedType.Name == "IAsyncEnumerable" && namedType.TypeArguments.Length == 1)
            {
                elementType = namedType.TypeArguments[0];
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
        // Keep the TypedConstant for proper formatting later
        return typedConstant;
    }

    private record DataSourceInfo(string MethodName, ITypeSymbol? ClassType, object?[]? Arguments);
}