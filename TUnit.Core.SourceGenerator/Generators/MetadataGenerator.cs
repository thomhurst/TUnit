using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating test metadata and registration
/// </summary>
public sealed class MetadataGenerator
{
    private readonly DataSourceGenerator _dataSourceGenerator;

    public MetadataGenerator(DataSourceGenerator dataSourceGenerator)
    {
        _dataSourceGenerator = dataSourceGenerator;
    }

    /// <summary>
    /// Generates test metadata for all test methods
    /// </summary>
    public void GenerateTestRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("var successCount = 0;");
        writer.AppendLine("var failedTests = new List<string>();");
        writer.AppendLine();

        foreach (var testInfo in testMethods)
        {
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();

            GenerateTestMetadata(writer, testInfo);

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

    /// <summary>
    /// Generates test metadata for a single test method
    /// </summary>
    public void GenerateSingleTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods without type arguments to avoid CS0453 errors
        if (testInfo.MethodSymbol.IsGenericMethod &&
            (testInfo.GenericTypeArguments == null || testInfo.GenericTypeArguments.Length == 0))
        {
            // Log a comment to help with debugging
            writer.AppendLine($"// Skipped generic method {testInfo.MethodSymbol.Name} - no type arguments provided");
            return;
        }

        writer.AppendLine("_testMetadata = new TestMetadata");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadata(writer, testInfo);
        GenerateTestAttributes(writer, testInfo);
        _dataSourceGenerator.GenerateDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GenerateClassDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GeneratePropertyDataSourceMetadata(writer, testInfo);
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateDelegateReferences(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("};");
    }

    private void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods without type arguments to avoid CS0453 errors
        if (testInfo.MethodSymbol.IsGenericMethod &&
            (testInfo.GenericTypeArguments == null || testInfo.GenericTypeArguments.Length == 0))
        {
            // Log a comment to help with debugging
            writer.AppendLine($"// Skipped generic method {testInfo.MethodSymbol.Name} - no type arguments provided");
            writer.AppendLine($"// Method has {testInfo.MethodSymbol.TypeParameters.Length} type parameters");
            writer.AppendLine($"// Method parameters: {string.Join(", ", testInfo.MethodSymbol.Parameters.Select(p => p.Type.ToDisplayString()))}");
            return;
        }

        // Debug output for generic methods with type arguments
        if (testInfo.MethodSymbol.IsGenericMethod && testInfo.GenericTypeArguments != null)
        {
            writer.AppendLine($"// Generating generic method {testInfo.MethodSymbol.Name} with type arguments: {string.Join(", ", testInfo.GenericTypeArguments.Select(t => t.ToDisplayString()))}");
        }

        writer.AppendLine("_allTests.Add(new TestMetadata");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadata(writer, testInfo);
        GenerateTestAttributes(writer, testInfo);
        _dataSourceGenerator.GenerateDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GenerateClassDataSourceMetadata(writer, testInfo);
        _dataSourceGenerator.GeneratePropertyDataSourceMetadata(writer, testInfo);
        GenerateParameterTypes(writer, testInfo);
        // Hook metadata is now generated by UnifiedHookMetadataGenerator
        GenerateEmptyHookMetadata(writer);
        GenerateDelegateReferences(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private static object? GetTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            // For array, return the first element if it exists
            return typedConstant.Values.Length > 0 ? typedConstant.Values[0].Value : null;
        }
        return typedConstant.Value;
    }

    private void GenerateBasicMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var testId = $"{className}.{methodName}";

        if (testInfo.MethodSymbol.Parameters.Any())
        {
            var paramTypes = string.Join(",", testInfo.MethodSymbol.Parameters.Select(p =>
                p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            testId += $"({paramTypes})";
        }

        writer.AppendLine($"TestId = \"{testId}\",");
        writer.AppendLine($"TestName = \"{methodName}\",");

        // Check if the type contains unresolved type parameters
        if (ContainsTypeParameter(testInfo.TypeSymbol))
        {
            // This shouldn't happen with proper filtering, but provide a fallback
            writer.AppendLine("TestClassType = typeof(object),");
        }
        else
        {
            writer.AppendLine($"TestClassType = typeof({className}),");
        }

        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // File location if available
        var location = testInfo.MethodSymbol.Locations.FirstOrDefault();
        if (location != null && location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            writer.AppendLine($"FilePath = @\"{lineSpan.Path}\",");
            writer.AppendLine($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
        }
    }

    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
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

        // Generate legacy DependsOn for backward compatibility
        var legacyDependencies = new List<string>();
        var testDependencies = new List<string>();

        foreach (var attr in dependsOnAttributes)
        {
            // Check if it's a generic DependsOnAttribute<T>
            if (attr.AttributeClass?.IsGenericType == true)
            {
                // Generic version: DependsOnAttribute<T>
                var typeArg = attr.AttributeClass.TypeArguments.FirstOrDefault();
                if (typeArg != null)
                {
                    var testName = GetTypedConstantValue(attr.ConstructorArguments.FirstOrDefault())?.ToString();
                    if (string.IsNullOrEmpty(testName))
                    {
                        // Depends on all tests in the class
                        testDependencies.Add($"TestDependency.FromClass(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                    }
                    else
                    {
                        // Depends on specific test in the class
                        testDependencies.Add($"TestDependency.FromClassAndMethod(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), \"{testName}\")");
                    }
                }
            }
            else
            {
                // Non-generic version
                var args = attr.ConstructorArguments;
                if (args is
                    [
                        { Type.Name: "String" } _
                    ])
                {
                    // DependsOnAttribute(string testName)
                    var testName = GetTypedConstantValue(args[0])?.ToString();
                    if (!string.IsNullOrEmpty(testName))
                    {
                        legacyDependencies.Add(testName!);
                        testDependencies.Add($"TestDependency.FromMethodName(\"{testName}\")");
                    }
                }
                else if (args is
                         [
                             { Type.Name: "Type" } _, ..
                         ])
                {
                    // DependsOnAttribute(Type testClass) or DependsOnAttribute(Type testClass, string testName)
                    if (GetTypedConstantValue(args[0]) is ITypeSymbol classType)
                    {
                        var className = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (args.Length == 1)
                        {
                            // Depends on all tests in the class
                            testDependencies.Add($"TestDependency.FromClass(typeof({className}))");
                        }
                        else if (args is
                                 [
                                     _, { Type.Name: "String" } _, ..
                                 ])
                        {
                            var testName = GetTypedConstantValue(args[1])?.ToString();
                            if (!string.IsNullOrEmpty(testName))
                            {
                                testDependencies.Add($"TestDependency.FromClassAndMethod(typeof({className}), \"{testName}\")");
                            }
                        }
                    }
                }
            }
        }

        // DependsOn array removed - using Dependencies property instead

        // Generate Dependencies array (new)
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

        // If this is a generic method with inferred type arguments, use the constructed method
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
                // If construction fails, fall back to original parameters
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

        // Check if any parameter contains unresolved type parameters
        var hasUnresolvedTypeParameters = parameters.Any(p => ContainsTypeParameter(p.Type));
        if (hasUnresolvedTypeParameters && testInfo.GenericTypeArguments == null)
        {
            // For methods with unresolved type parameters, we can't generate typeof() at compile time
            writer.AppendLine("ParameterTypes = Type.EmptyTypes,");
            writer.AppendLine("TestMethodParameterTypes = Array.Empty<string>(),");
            return;
        }

        // Generate ParameterTypes
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

        // Generate TestMethodParameterTypes (string array for dependency matching)
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

    private void GenerateDelegateReferences(CodeWriter writer, TestMethodMetadata testInfo)
    {
        testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate instance factory inline
        GenerateInlineInstanceFactory(writer, testInfo);

        // Generate test invoker inline
        GenerateInlineTestInvoker(writer, testInfo);

        // Generate property setters inline
        GenerateInlinePropertySetters(writer, testInfo);

        // Generate property injections with embedded factories
        GeneratePropertyInjections(writer, testInfo);
    }

    private void GenerateEmptyHookMetadata(CodeWriter writer)
    {
        // Hooks are now handled by UnifiedHookMetadataGenerator
        // Generate empty hook arrays for compatibility
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

    // This method is no longer used - replaced by DataSourceGenerator.GeneratePropertyDataSourceMetadata
    // Kept for reference only
    private void GeneratePropertyDataSources_OLD(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var propertyDataSources = new List<(string name, ITypeSymbol type, AttributeData attr)>();

        // Find all properties with data source attributes
        foreach (var member in testInfo.TypeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Check for ClassDataSource attribute
            var classDataSourceAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ClassDataSourceAttribute");
            if (classDataSourceAttr != null)
            {
                propertyDataSources.Add((member.Name, member.Type, classDataSourceAttr));
                continue;
            }

            // Check for MethodDataSource attribute on properties
            var methodDataSourceAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
            if (methodDataSourceAttr != null)
            {
                propertyDataSources.Add((member.Name, member.Type, methodDataSourceAttr));
                continue;
            }

            // Check for other data source attributes (e.g., custom attributes)
            var dataSourceAttrs = member.GetAttributes()
                .Where(a => a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true)
                .ToList();
            foreach (var attr in dataSourceAttrs)
            {
                propertyDataSources.Add((member.Name, member.Type, attr));
            }
        }

        if (!propertyDataSources.Any())
        {
            writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
            return;
        }

        writer.AppendLine("PropertyDataSources = new PropertyDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var (propName, propType, attr) in propertyDataSources)
        {
            writer.AppendLine("new PropertyDataSource");
            writer.AppendLine("{");
            writer.Indent();

            writer.AppendLine($"PropertyName = \"{propName}\",");
            writer.AppendLine($"PropertyType = typeof({propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");

            // Generate the appropriate data source based on attribute type
            GeneratePropertyDataSourceInstance(writer, attr, testInfo.TypeSymbol, propName);

            writer.Unindent();
            writer.AppendLine("},");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GeneratePropertyDataSourceInstance(CodeWriter writer, AttributeData attr, ITypeSymbol classType, string propertyName)
    {
        if (attr.AttributeClass?.Name == "ClassDataSourceAttribute")
        {
            // ClassDataSource creates instances of the specified type
            var sharedType = GetSharedTypeFromAttribute(attr);
            var isShared = sharedType != "None";

            // Get the type to instantiate from the generic attribute
            if (attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 } namedType)
            {
                var dataType = namedType.TypeArguments[0];
                writer.AppendLine($"DataSource = new DelegateDataSource(() => new object?[][] {{ new object?[] {{ new {dataType.ToDisplayString()}() }} }}, {isShared.ToString().ToLower()})");
            }
            else
            {
                // Non-generic ClassDataSource - need to get the type from constructor arguments
                var typeArg = attr.ConstructorArguments.FirstOrDefault();
                if (GetTypedConstantValue(typeArg) is ITypeSymbol typeSymbol)
                {
                    writer.AppendLine($"DataSource = new DelegateDataSource(() => new object?[][] {{ new object?[] {{ new {typeSymbol.ToDisplayString()}() }} }}, {isShared.ToString().ToLower()})");
                }
            }
        }
        else if (attr.AttributeClass?.Name == "MethodDataSourceAttribute")
        {
            // MethodDataSource references a method
            var methodName = GetTypedConstantValue(attr.ConstructorArguments.FirstOrDefault())?.ToString();
            if (!string.IsNullOrEmpty(methodName))
            {
                var member = classType.GetMembers(methodName!).FirstOrDefault();
                if (member is IMethodSymbol method)
                {
                    if (IsAsyncEnumerableType(method.ReturnType))
                    {
                        // Check if method accepts CancellationToken
                        var hasCancellationToken = method.Parameters.Any(p => p.Type.Name == "CancellationToken");
                        if (hasCancellationToken)
                        {
                            writer.AppendLine($"DataSource = new AsyncDelegateDataSource((ct) => {classType.ToDisplayString()}.{methodName}(ct), false)");
                        }
                        else
                        {
                            writer.AppendLine($"DataSource = new AsyncDelegateDataSource((ct) => {classType.ToDisplayString()}.{methodName}(), false)");
                        }
                    }
                    else if (IsTaskOfEnumerableType(method.ReturnType))
                    {
                        writer.AppendLine($"DataSource = new TaskDelegateDataSource(() => {classType.ToDisplayString()}.{methodName}(), false)");
                    }
                    else if (IsFuncOfTupleType(method.ReturnType))
                    {
                        // Special case: method returns Func<tuple> - need to invoke the func and wrap in array
                        writer.AppendLine($"DataSource = new DelegateDataSource(() => {{ var func = {classType.ToDisplayString()}.{methodName}(); var result = func(); return new object?[][] {{ new object?[] {{ result }} }}; }}, false)");
                    }
                    else if (IsEnumerableOfObjectArrayType(method.ReturnType))
                    {
                        // Method returns IEnumerable<object?[]> - use directly
                        writer.AppendLine($"DataSource = new DelegateDataSource(() => {classType.ToDisplayString()}.{methodName}(), false)");
                    }
                    else
                    {
                        // For other return types (single values), wrap in array
                        writer.AppendLine($"DataSource = new DelegateDataSource(() => new object?[][] {{ new object?[] {{ {classType.ToDisplayString()}.{methodName}() }} }}, false)");
                    }
                }
                else
                {
                    writer.AppendLine($"DataSource = new DelegateDataSource(() => {classType.ToDisplayString()}.{methodName}(), false)");
                }
            }
        }
        else
        {
            // For custom data attributes (like ArgumentsAttribute), get the values from the attribute
            if (attr.AttributeClass?.Name == "ArgumentsAttribute" && attr.ConstructorArguments.Length > 0)
            {
                // ArgumentsAttribute provides the values directly
                var values = attr.ConstructorArguments.Select(arg => TypedConstantParser.GetRawTypedConstantValue(arg)).ToList();
                var valuesStr = string.Join(", ", values);
                writer.AppendLine($"DataSource = new DelegateDataSource(() => new object?[][] {{ new object?[] {{ {valuesStr} }} }}, false)");
            }
            else
            {
                // For other data attributes, just return empty for now
                writer.AppendLine("DataSource = new DelegateDataSource(() => new object?[][] { new object?[] { null } }, false)");
            }
        }
    }

    private bool IsAsyncEnumerableType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var fullName = namedType.ToDisplayString();
        return fullName.StartsWith("System.Collections.Generic.IAsyncEnumerable<") ||
               fullName.StartsWith("IAsyncEnumerable<");
    }

    private bool IsTaskOfEnumerableType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (namedType.Name != "Task" || namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        var innerType = namedType.TypeArguments[0];
        var innerTypeName = innerType.ToDisplayString();

        return innerTypeName.Contains("IEnumerable<") ||
               innerTypeName.Contains("List<") ||
               innerTypeName.Contains("[]");
    }

    private bool IsFuncOfTupleType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        if (!namedType.Name.StartsWith("Func") || namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        var returnType = namedType.TypeArguments[0];
        return returnType is INamedTypeSymbol { IsTupleType: true };
    }

    private bool IsEnumerableOfObjectArrayType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var typeName = namedType.ToDisplayString();
        return typeName.Contains("IEnumerable<object[]>") ||
               typeName.Contains("IEnumerable<object?[]>") ||
               typeName.Contains("List<object[]>") ||
               typeName.Contains("List<object?[]>");
    }

    private string GetSharedTypeFromAttribute(AttributeData attr)
    {
        // Look for Shared property in named arguments
        var sharedArg = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared");
        if (sharedArg.Key != null)
        {
            var value = sharedArg.Value.Value?.ToString();
            return value ?? "None";
        }
        return "None";
    }


    private void GenerateAttributeTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Collect all attributes that implement ITestDiscoveryEventReceiver
        var discoveryAttributes = new List<AttributeData>();

        // Method attributes
        foreach (var attr in testInfo.MethodSymbol.GetAttributes())
        {
            if (attr.AttributeClass != null && IsDiscoveryEventReceiverAttribute(attr.AttributeClass))
            {
                discoveryAttributes.Add(attr);
            }
        }

        // Class attributes
        foreach (var attr in testInfo.TypeSymbol.GetAttributes())
        {
            if (attr.AttributeClass != null && IsDiscoveryEventReceiverAttribute(attr.AttributeClass))
            {
                discoveryAttributes.Add(attr);
            }
        }

        // Assembly attributes
        if (testInfo.TypeSymbol.ContainingAssembly != null)
        {
            foreach (var attr in testInfo.TypeSymbol.ContainingAssembly.GetAttributes())
            {
                if (attr.AttributeClass != null && IsDiscoveryEventReceiverAttribute(attr.AttributeClass))
                {
                    discoveryAttributes.Add(attr);
                }
            }
        }

        if (discoveryAttributes.Any())
        {
            writer.AppendLine("AttributeFactory = () => new Attribute[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var attr in discoveryAttributes)
            {
                // Generate attribute instantiation using TypedConstantParser
                GenerateAttributeInstantiation(writer, attr);
                writer.AppendLine(",");
            }

            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("AttributeFactory = null,");
        }
    }

    private void GenerateAttributeInstantiation(CodeWriter writer, AttributeData attributeData)
    {
        var attributeType = attributeData.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Handle constructor arguments
        if (attributeData.ConstructorArguments.Length == 0)
        {
            writer.Append($"new {attributeType}()");
        }
        else
        {
            writer.Append($"new {attributeType}(");
            for (var i = 0; i < attributeData.ConstructorArguments.Length; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                writer.Append(TypedConstantParser.GetRawTypedConstantValue(attributeData.ConstructorArguments[i]));
            }
            writer.Append(")");
        }

        // Handle named arguments (properties)
        if (attributeData.NamedArguments.Length > 0)
        {
            writer.Append(" { ");
            for (var i = 0; i < attributeData.NamedArguments.Length; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                var namedArg = attributeData.NamedArguments[i];
                writer.Append($"{namedArg.Key} = {TypedConstantParser.GetRawTypedConstantValue(namedArg.Value)}");
            }
            writer.Append(" }");
        }
    }

    private bool IsDiscoveryEventReceiverAttribute(INamedTypeSymbol attributeClass)
    {
        // Check if the attribute implements ITestDiscoveryEventReceiver
        return attributeClass.AllInterfaces.Any(i =>
            i.Name == "ITestDiscoveryEventReceiver" &&
            i.ContainingNamespace?.ToDisplayString() == "TUnit.Core.Interfaces");
    }

    private void GenerateInlineInstanceFactory(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Get all non-static properties that have data source attributes
        var propertiesWithDataSources = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null && !p.IsStatic && p.GetAttributes()
                .Any(a => a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true
                    || a.AttributeClass?.Name == "ClassDataSourceAttribute"
                    || a.AttributeClass?.Name == "MethodDataSourceAttribute"
                    || a.AttributeClass?.Name == "ArgumentsAttribute"))
            .ToList();

        if (propertiesWithDataSources.Any())
        {
            // Generate a factory that expects property values to be passed via a dictionary in args
            writer.AppendLine("InstanceFactory = args =>");
            writer.AppendLine("{");
            writer.Indent();

            // Constructor arguments come first
            var constructorArgs = GenerateConstructorArgs(testInfo);

            // The runtime will pass property values as the last argument
            writer.AppendLine("var propertyValues = args.Length > 0 && args[args.Length - 1] is Dictionary<string, object?> dict ? dict : new Dictionary<string, object?>();");
            writer.AppendLine($"return new {className}({constructorArgs})");
            writer.AppendLine("{");
            writer.Indent();

            // Generate property initializers for ALL properties with data sources
            foreach (var prop in propertiesWithDataSources)
            {
                var propName = prop.Name;
                var propType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                writer.AppendLine($"{propName} = propertyValues.TryGetValue(\"{propName}\", out var _{propName}) ? ({propType})_{propName}! : default!,");
            }

            writer.Unindent();
            writer.AppendLine("};");

            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            // Standard instantiation for classes without properties with data sources
            writer.AppendLine($"InstanceFactory = args => new {className}({GenerateConstructorArgs(testInfo)}),");
        }
    }

    private void GenerateInlineTestInvoker(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Cast instance to correct type
        writer.AppendLine($"var typedInstance = ({className})instance;");

        // Check if this is a generic method that needs special handling
        if (testInfo.MethodSymbol.IsGenericMethod && testInfo.GenericTypeArguments != null &&
            testInfo.GenericTypeArguments.Length == testInfo.MethodSymbol.TypeParameters.Length)
        {
            try
            {
                // For generic methods with inferred types, generate a constructed method call
                var typeArgs = string.Join(", ", testInfo.GenericTypeArguments.Select(t =>
                    t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var constructedMethod = testInfo.MethodSymbol.Construct(testInfo.GenericTypeArguments);

                // Generate parameter casting using the constructed method's parameters
                var parameters = constructedMethod.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
            }

            // Generate method call with explicit type arguments
            var argList = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(i => $"arg{i}"));
            if (isAsync)
            {
                writer.AppendLine($"await typedInstance.{methodName}<{typeArgs}>({argList});");
            }
            else
            {
                writer.AppendLine($"typedInstance.{methodName}<{typeArgs}>({argList});");
                writer.AppendLine("await Task.CompletedTask;");
            }
            }
            catch
            {
                // If construction fails, fall back to non-generic handling
                GenerateNonGenericMethodCall(writer, testInfo);
            }
        }
        else
        {
            GenerateNonGenericMethodCall(writer, testInfo);
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateNonGenericMethodCall(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        // Generate parameter casting
        var parameters = testInfo.MethodSymbol.Parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
        }

        // Generate method call
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
    }

    private void GenerateInlinePropertySetters(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // PropertySetters are no longer needed since all properties are set during construction
        writer.AppendLine("PropertySetters = new Dictionary<string, Action<object, object?>>(),");
    }

    private void GeneratePropertyInjections(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // PropertyInjections are no longer needed since all properties are set during construction
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
    }

    private bool HasRequiredPropertiesWithDataSource(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.IsRequired && p.GetAttributes()
                .Any(a => a.AttributeClass?.AllInterfaces
                    .Any(i => i.Name == "IDataAttribute") == true));
    }

    private List<IPropertySymbol> GetInitOnlyPropertiesWithDataSource(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod is { IsInitOnly: true } && p.GetAttributes()
                .Any(a => a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true
                    || a.AttributeClass?.Name == "ClassDataSourceAttribute"
                    || a.AttributeClass?.Name == "MethodDataSourceAttribute"))
            .ToList();
    }

    private string GetSafeFactoryMethodName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");
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
}
