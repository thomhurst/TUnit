using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

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
        EmitDataCombinationGenerator(writer, methodSymbol, typeSymbol, methodGuid, null);
    }

    public static void EmitDataCombinationGenerator(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        string methodGuid,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine($"private async IAsyncEnumerable<TestDataCombination> GenerateCombinations_{methodGuid}(string testSessionId)");
        writer.AppendLine("{");
        writer.Indent();

        // Use stored attributes if available (for generic types), otherwise get them directly
        var methodDataSources = testMethodMetadata?.MethodAttributes.IsDefault == false 
            ? testMethodMetadata.MethodAttributes.Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass)).ToImmutableArray()
            : GetDataSourceAttributes(methodSymbol);
        var classDataSources = GetDataSourceAttributes(typeSymbol);
        var propertyDataSources = GetPropertyDataSources(typeSymbol);

        // Debug: Log what we found at compile time
        writer.AppendLine($"// Compile-time debug: Method '{methodSymbol.Name}' has {methodDataSources.Length} data source attributes");
        writer.AppendLine($"// Compile-time debug: Method has {methodSymbol.GetAttributes().Length} total attributes");
        foreach (var attr in methodSymbol.GetAttributes())
        {
            writer.AppendLine($"// - Attribute: {attr.AttributeClass?.Name ?? "Unknown"}");
        }

        // Get repeat count from RepeatAttribute
        var repeatCount = GetRepeatCount(methodSymbol, typeSymbol);
        
        // Debug: Log all attributes on the method
        writer.AppendLine($"// Method: {methodSymbol.Name}");
        writer.AppendLine($"// All method attributes count: {methodSymbol.GetAttributes().Length}");
        foreach (var attr in methodSymbol.GetAttributes())
        {
            writer.AppendLine($"// Method attribute: {attr.AttributeClass?.Name}");
        }

        // Check if we have generic types that need resolution
        var hasGenericTypes = testMethodMetadata?.IsGenericType == true;
        var hasGenericMethods = testMethodMetadata?.IsGenericMethod == true;
        
        if (hasGenericTypes || hasGenericMethods)
        {
            // For generic types/methods, we need to resolve types based on data
            EmitGenericTypeResolution(writer, methodSymbol, typeSymbol, methodDataSources, classDataSources, propertyDataSources, repeatCount, testMethodMetadata);
        }
        else if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
        {
            // No data sources, but might have repeat
            for (var repeatIndex = 0; repeatIndex <= repeatCount; repeatIndex++)
            {
                writer.AppendLine($"yield return new TestDataCombination {{ RepeatIndex = {repeatIndex} }};");
            }
        }
        else
        {
            // All data sources are now handled at compile time
            EmitCompileTimeCombinations(writer, methodSymbol, typeSymbol, methodDataSources, classDataSources, propertyDataSources, repeatCount);
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
        ImmutableArray<PropertyWithDataSource> propertyDataSources,
        int repeatCount)
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
        
        // Runtime helpers now handle instance data source property initialization

        EmitMethodDataCombinations(writer, methodDataSources, methodSymbol, typeSymbol);
        EmitClassDataCombinations(writer, classDataSources, methodSymbol, typeSymbol);
        EmitPropertyDataCombinations(writer, propertyDataSources, methodSymbol, typeSymbol);

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
        writer.AppendLine("// Apply repeat for error cases too");
        writer.AppendLine($"for (var repeatIndex = 0; repeatIndex <= {repeatCount}; repeatIndex++)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("yield return new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = errorCombination.DataGenerationException,");
        writer.AppendLine("DisplayName = errorCombination.DisplayName,");
        writer.AppendLine("RepeatIndex = repeatIndex");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("else");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Apply repeat if specified");
        writer.AppendLine($"for (var repeatIndex = 0; repeatIndex <= {repeatCount}; repeatIndex++)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("foreach (var combination in allCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Clone the combination with the repeat index");
        writer.AppendLine("yield return new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("ClassDataFactories = combination.ClassDataFactories,");
        writer.AppendLine("MethodDataFactories = combination.MethodDataFactories,");
        writer.AppendLine("ClassDataSourceIndex = combination.ClassDataSourceIndex,");
        writer.AppendLine("MethodDataSourceIndex = combination.MethodDataSourceIndex,");
        writer.AppendLine("ClassLoopIndex = combination.ClassLoopIndex,");
        writer.AppendLine("MethodLoopIndex = combination.MethodLoopIndex,");
        writer.AppendLine("PropertyValueFactories = combination.PropertyValueFactories,");
        writer.AppendLine("DataGenerationException = combination.DataGenerationException,");
        writer.AppendLine("DisplayName = combination.DisplayName,");
        writer.AppendLine("RepeatIndex = repeatIndex");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitMethodDataCombinations(CodeWriter writer, ImmutableArray<AttributeData> methodDataSources, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// Method data sources");
        writer.AppendLine("int methodDataSourceCounter = 0;");
        writer.AppendLine("int classDataSourceCounter = 0;");
        for (var i = 0; i < methodDataSources.Length; i++)
        {
            var attr = methodDataSources[i];
            EmitDataSourceCombination(writer, attr, "methodCombinations", isClassLevel: false, methodSymbol, typeSymbol);
        }
    }

    private static void EmitClassDataCombinations(CodeWriter writer, ImmutableArray<AttributeData> classDataSources, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine();
        writer.AppendLine("// Class data sources");
        writer.AppendLine("classDataSourceCounter = 0;");
        writer.AppendLine("methodDataSourceCounter = 0;");
        for (var i = 0; i < classDataSources.Length; i++)
        {
            var attr = classDataSources[i];
            EmitDataSourceCombination(writer, attr, "classCombinations", isClassLevel: true, methodSymbol, typeSymbol);
        }
    }

    private static void EmitPropertyDataCombinations(CodeWriter writer, ImmutableArray<PropertyWithDataSource> propertyDataSources, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        if (!propertyDataSources.Any())
            return;

        writer.AppendLine();
        writer.AppendLine("// Property data sources");
        writer.AppendLine("var propertyValues = new Dictionary<string, Func<Task<object?>>>();");
        writer.AppendLine();
        
        // Generate testInformation for property data sources  
        writer.AppendLine("// Create TestInformation for property data sources");
        writer.Append("var propertyTestInformation = ");
        TestInformationGenerator.GenerateTestInformation(writer, methodSymbol, typeSymbol);
        writer.AppendLine(";");
        writer.AppendLine();

        foreach (var propData in propertyDataSources)
        {
            EmitPropertyDataSource(writer, propData, typeSymbol, methodSymbol);
        }

        writer.AppendLine("propertyCombinations.Add(new TestDataCombination { PropertyValueFactories = propertyValues });");
    }

    private static void EmitDataSourceCombination(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("{");
        writer.Indent();

        // Emit code to get the current indices
        writer.AppendLine("// Get current indices and increment the appropriate counter");
        writer.AppendLine("var currentClassIndex = classDataSourceCounter;");
        writer.AppendLine("var currentMethodIndex = methodDataSourceCounter;");

        if (isClassLevel)
        {
            writer.AppendLine("classDataSourceCounter++;");
        }
        else
        {
            writer.AppendLine("methodDataSourceCounter++;");
        }

        if (attr.AttributeClass == null)
        {
            EmitEmptyCombination(writer, listName);
        }
        else
        {
            var fullyQualifiedName = attr.AttributeClass.GloballyQualifiedNonGeneric();

            if (fullyQualifiedName == "global::TUnit.Core.ArgumentsAttribute")
            {
                EmitArgumentsAttribute(writer, attr, listName, isClassLevel, methodSymbol, typeSymbol);
            }
            else if (fullyQualifiedName == "global::TUnit.Core.MethodDataSourceAttribute")
            {
                EmitMethodDataSource(writer, attr, listName, isClassLevel, typeSymbol, methodSymbol);
            }
            else if (fullyQualifiedName == "global::TUnit.Core.InstanceMethodDataSourceAttribute")
            {
                EmitInstanceMethodDataSource(writer, attr, listName, isClassLevel, typeSymbol, methodSymbol);
            }
            else if (attr.AttributeClass?.IsOrInherits("global::TUnit.Core.InstanceMethodDataSourceAttribute") == true)
            {
                // Handle typed InstanceMethodDataSourceAttribute<T>
                EmitTypedInstanceMethodDataSource(writer, attr, listName, isClassLevel, typeSymbol, methodSymbol);
            }
            else if (IsAsyncDataSourceGeneratorAttribute(attr.AttributeClass))
            {
                // Check if it's an async untyped data source generator
                if (IsAsyncUntypedDataSourceGeneratorAttribute(attr.AttributeClass))
                {
                    EmitAsyncUntypedDataSourceGeneratorAttribute(writer, attr, listName, isClassLevel, methodSymbol, typeSymbol);
                }
                else
                {
                    // It's a typed AsyncDataSourceGeneratorAttribute (including DataSourceGeneratorAttribute)
                    EmitAsyncDataSourceGeneratorAttribute(writer, attr, listName, isClassLevel, methodSymbol, typeSymbol);
                }
            }
            else
            {
                writer.AppendLine($"// Unsupported data source: {fullyQualifiedName}");
                EmitEmptyCombination(writer, listName);
            }
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void EmitArgumentsAttribute(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// ArgumentsAttribute");

        try
        {
            var formattedArgs = new List<string>();

            // Get the parameter types - for method data sources, use method parameters; for class data sources, use constructor parameters
            var parameters = isClassLevel
                ? typeSymbol.Constructors.FirstOrDefault(c => !c.IsStatic)?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty
                : methodSymbol.Parameters;

            if (attr.ConstructorArguments is { IsDefaultOrEmpty: true }
                or [{ IsNull: true }])
            {
                formattedArgs = ["null"];
            }
            else if (attr.ConstructorArguments is
                [
                    { Kind: TypedConstantKind.Array }
                ])
            {
                var values = attr.ConstructorArguments[0].Values;
                formattedArgs.AddRange(ProcessArgumentsForParams(values, parameters));
            }
            else
            {
                formattedArgs.AddRange(ProcessArgumentsForParams(attr.ConstructorArguments, parameters));
            }

            writer.AppendLine($"{listName}.Add(new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();

            if (isClassLevel)
            {
                writer.AppendLine($"ClassDataFactories = new Func<Task<object?>>[] {{ {string.Join(", ", formattedArgs.Select(arg => $"() => Task.FromResult<object?>({arg})"))} }},");
            }
            else
            {
                writer.AppendLine($"MethodDataFactories = new Func<Task<object?>>[] {{ {string.Join(", ", formattedArgs.Select(arg => $"() => Task.FromResult<object?>({arg})"))} }},");
            }

            // Always write both indices
            writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
            writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

            // Always write both loop indices (0 for Arguments attribute since it's not a loop)
            writer.AppendLine("ClassLoopIndex = 0,");
            writer.AppendLine("MethodLoopIndex = 0,");

            writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>()");
            writer.Unindent();
            writer.AppendLine("});");
        }
        catch
        {
            writer.AppendLine("// Error processing ArgumentsAttribute");
            EmitEmptyCombination(writer, listName);
        }
    }

    private static void EmitMethodDataSource(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        writer.AppendLine("// MethodDataSourceAttribute");

        if (attr.ConstructorArguments.Length < 1)
        {
            EmitEmptyCombination(writer, listName);
            return;
        }

        // Method name can be in different positions depending on overload
        string? methodName = null;
        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol } _, _
            ])
        {
            // MethodDataSource(Type, string) overload
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            // MethodDataSource(string) overload
            methodName = attr.ConstructorArguments[0].Value?.ToString();
        }

        if (string.IsNullOrEmpty(methodName))
        {
            EmitEmptyCombination(writer, listName);
            return;
        }

        // Determine which type contains the method
        var methodClass = GetMethodClass(attr, typeSymbol);

        // Find the method on the type
        var dataSourceMethod = methodClass
            .GetMembers(methodName!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (dataSourceMethod == null)
        {
            writer.AppendLine($"// Method '{methodName}' not found");
            EmitEmptyCombination(writer, listName);
            return;
        }

        var isStatic = dataSourceMethod.IsStatic;

        writer.AppendLine($"// Calling method: {methodName} (static: {isStatic})");

        if (isStatic)
        {
            EmitStaticMethodDataSource(writer, methodName!, listName, isClassLevel, methodClass, dataSourceMethod, attr, methodSymbol);
        }
        else
        {
            EmitInstanceMethodDataSource(writer, attr, listName, isClassLevel, methodClass, methodSymbol);
        }
    }

    private static ITypeSymbol GetMethodClass(AttributeData methodDataAttribute, INamedTypeSymbol typeContainingAttribute)
    {
        // For InstanceMethodDataSource (including generic variants), always use the test class
        if (methodDataAttribute.AttributeClass?.IsOrInherits("global::TUnit.Core.InstanceMethodDataSourceAttribute") == true)
        {
            return typeContainingAttribute;
        }

        if (methodDataAttribute.AttributeClass?.IsGenericType is true)
        {
            return methodDataAttribute.AttributeClass.TypeArguments[0];
        }

        if (methodDataAttribute.ConstructorArguments.Length is 2)
        {
            return (ITypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
        }

        return typeContainingAttribute;
    }

    private static void EmitStaticMethodDataSource(CodeWriter writer, string methodName, string listName, bool isClassLevel, ITypeSymbol typeSymbol, IMethodSymbol dataSourceMethod, AttributeData attr, IMethodSymbol methodSymbol)
    {
        var fullyQualifiedTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Get the Arguments property from the attribute
        var argumentsProperty = attr.NamedArguments.FirstOrDefault(x => x.Key == "Arguments");
        var hasArguments = argumentsProperty is { Key: not null, Value.IsNull: false };

        // Build the method call with arguments if any
        var methodCall = $"{fullyQualifiedTypeName}.{methodName}(";
        if (hasArguments && argumentsProperty.Value.Kind == TypedConstantKind.Array)
        {
            var arguments = new List<string>();
            foreach (var arg in argumentsProperty.Value.Values)
            {
                arguments.Add(FormatConstantValue(arg));
            }
            methodCall += string.Join(", ", arguments);
        }
        methodCall += ")";

        // Check if the method returns an enumerable type
        var isEnumerable = IsEnumerable(dataSourceMethod.ReturnType);
        var isAsyncEnumerable = IsAsyncEnumerable(dataSourceMethod.ReturnType);

        if (isEnumerable || isAsyncEnumerable)
        {
            // Method returns enumerable - iterate over it
            writer.AppendLine($"var dataEnumerable = {methodCall};");
            writer.AppendLine("int classLoopCounter = 0;");
            writer.AppendLine("int methodLoopCounter = 0;");

            if (isAsyncEnumerable)
            {
                writer.AppendLine("await foreach (var data in dataEnumerable)");
            }
            else
            {
                writer.AppendLine("foreach (var data in dataEnumerable)");
            }
            writer.AppendLine("{");
            writer.Indent();
            
            // Don't invoke Func here - let ProcessTestDataSource handle it
            writer.AppendLine("var processedData = data;");

            writer.AppendLine($"{listName}.Add(new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();

            if (isClassLevel)
            {
                // For class-level data sources, pass constructor parameter count
                var ctorParamCount = methodSymbol.ContainingType.Constructors
                    .FirstOrDefault(c => !c.IsStatic)?.Parameters.Length ?? 0;
                writer.AppendLine($"ClassDataFactories = global::TUnit.Core.Helpers.DataSourceHelpers.ProcessTestDataSource(processedData, {ctorParamCount}),");
            }
            else
            {
                // For method-level data sources, pass method parameter count
                writer.AppendLine($"MethodDataFactories = global::TUnit.Core.Helpers.DataSourceHelpers.ProcessTestDataSource(processedData, {methodSymbol.Parameters.Length}),");
            }

            // Always write both indices
            writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
            writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

            // Always write both loop indices
            if (isClassLevel)
            {
                writer.AppendLine("ClassLoopIndex = classLoopCounter++,");
                writer.AppendLine("MethodLoopIndex = methodLoopCounter = 0,");
            }
            else
            {
                writer.AppendLine("ClassLoopIndex = classLoopCounter,");
                writer.AppendLine("MethodLoopIndex = methodLoopCounter++,");
            }

            writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>()");
            writer.Unindent();
            writer.AppendLine("});");

            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            // Method returns single value
            writer.AppendLine($"var dataValue = {methodCall};");
            
            // Don't invoke Func here - let ProcessTestDataSource handle it
            
            writer.AppendLine($"{listName}.Add(new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();

            if (isClassLevel)
            {
                // For class-level data sources, pass constructor parameter count
                var ctorParamCount = methodSymbol.ContainingType.Constructors
                    .FirstOrDefault(c => !c.IsStatic)?.Parameters.Length ?? 0;
                writer.AppendLine($"ClassDataFactories = global::TUnit.Core.Helpers.DataSourceHelpers.ProcessTestDataSource(dataValue, {ctorParamCount}),");
            }
            else
            {
                // For method-level data sources, pass method parameter count
                writer.AppendLine($"MethodDataFactories = global::TUnit.Core.Helpers.DataSourceHelpers.ProcessTestDataSource(dataValue, {methodSymbol.Parameters.Length}),");
            }

            // Always write both indices
            writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
            writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

            // Always write both loop indices (0 since it's a single value)
            writer.AppendLine("ClassLoopIndex = 0,");
            writer.AppendLine("MethodLoopIndex = 0,");

            writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>()");
            writer.Unindent();
            writer.AppendLine("});");
        }
    }

    private static void EmitInstanceMethodDataSource(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, ITypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        // Extract method name from attribute
        string? methodName = null;
        ITypeSymbol? targetType = null;
        
        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol } _, _
            ])
        {
            targetType = (ITypeSymbol)attr.ConstructorArguments[0].Value;
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            methodName = attr.ConstructorArguments[0].Value?.ToString();
            targetType = typeSymbol;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            writer.AppendLine($"// Error: No method name provided for InstanceMethodDataSource");
            EmitEmptyCombination(writer, listName);
            return;
        }

        writer.AppendLine($"// Instance method data source: {methodName}");
        writer.AppendLine("// Instance methods are deferred to runtime execution");
        
        // Generate deferred execution that will be called at runtime
        writer.AppendLine($"var deferredInstanceMethod = new Func<Task<object?[][]>>(async () => {{");
        writer.Indent();
        writer.AppendLine("// This will be executed at runtime when the test instance is available");
        writer.AppendLine("throw new NotSupportedException(");
        writer.AppendLine($"    \"Untyped instance method '{methodName}' requires manual implementation. \" +");
        writer.AppendLine("    \"Please use InstanceMethodDataSourceAttribute<T> for type-safe generic inference.\");");
        writer.Unindent();
        writer.AppendLine("});");
        
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"MethodDataFactories = new Func<Task<object?>>[] {{ () => deferredInstanceMethod().ContinueWith(t => (object?)t.Result) }},");
        writer.AppendLine($"DisplayName = \"Instance method: {methodName}\",");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        writer.AppendLine("ResolvedGenericTypes = new Dictionary<string, Type>()");
        writer.Unindent();
        writer.AppendLine("});");
    }

    private static void EmitTypedInstanceMethodDataSource(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, ITypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        // Extract method name from attribute
        string? methodName = null;
        ITypeSymbol? targetType = null;
        
        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol } _, _
            ])
        {
            targetType = (ITypeSymbol)attr.ConstructorArguments[0].Value;
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            methodName = attr.ConstructorArguments[0].Value?.ToString();
            targetType = typeSymbol;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            writer.AppendLine($"// Error: No method name provided for typed InstanceMethodDataSource");
            EmitEmptyCombination(writer, listName);
            return;
        }

        // Extract type information from the generic attribute for type inference
        var attributeClass = attr.AttributeClass;
        if (attributeClass?.IsGenericType == true)
        {
            var typeArgs = attributeClass.TypeArguments;
            if (typeArgs.Length > 0)
            {
                writer.AppendLine($"// Typed instance method data source: {methodName}");
                writer.AppendLine("// Type inference from InstanceMethodDataSourceAttribute<T>");
                
                // Generate type resolution for generics
                writer.AppendLine("var instanceMethodTypes = new Dictionary<string, Type>();");
                
                // Map type arguments to generic parameters
                if (methodSymbol.IsGenericMethod)
                {
                    for (int i = 0; i < Math.Min(typeArgs.Length, methodSymbol.TypeParameters.Length); i++)
                    {
                        var typeParam = methodSymbol.TypeParameters[i];
                        var typeArg = typeArgs[i];
                        writer.AppendLine($"instanceMethodTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                    }
                }
                else if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
                {
                    for (int i = 0; i < Math.Min(typeArgs.Length, namedTypeSymbol.TypeParameters.Length); i++)
                    {
                        var typeParam = namedTypeSymbol.TypeParameters[i];
                        var typeArg = typeArgs[i];
                        writer.AppendLine($"instanceMethodTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                    }
                }
                
                // Validate constraints if we have generic parameters
                if (methodSymbol is { IsGenericMethod: true, TypeParameters.Length: > 0 })
                {
                    var typeArgStrings = typeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    EmitGenericConstraintValidation(writer, methodSymbol.TypeParameters, typeArgStrings, "method");
                }
                else if (typeSymbol is INamedTypeSymbol { IsGenericType: true, TypeParameters.Length: > 0 } namedTypeForConstraints)
                {
                    var typeArgStrings = typeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    EmitGenericConstraintValidation(writer, namedTypeForConstraints.TypeParameters, typeArgStrings, "class");
                }
                
                writer.AppendLine($"{listName}.Add(new TestDataCombination");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"MethodDataFactories = new Func<Task<object?>>[] {{ () => Task.FromResult<object?>(\"Instance method deferred: {methodName}\") }},");
                var typeNames = string.Join(", ", typeArgs.Select(t => t.Name));
                writer.AppendLine($"DisplayName = \"Instance method<{typeNames}>: {methodName}\",");
                writer.AppendLine("ClassDataSourceIndex = 0,");
                writer.AppendLine("MethodDataSourceIndex = 0,");
                writer.AppendLine("ClassLoopIndex = 0,");
                writer.AppendLine("MethodLoopIndex = 0,");
                writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
                writer.AppendLine("ResolvedGenericTypes = instanceMethodTypes");
                writer.Unindent();
                writer.AppendLine("});");
            }
            else
            {
                writer.AppendLine($"// Error: No type arguments found for InstanceMethodDataSourceAttribute");
                EmitEmptyCombination(writer, listName);
            }
        }
        else
        {
            writer.AppendLine($"// Error: InstanceMethodDataSourceAttribute is not generic");
            EmitEmptyCombination(writer, listName);
        }
    }

    private static void EmitPropertyDataSource(CodeWriter writer, PropertyWithDataSource propData, INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        try
        {
            var propertyName = propData.Property.Name;
            var attr = propData.DataSourceAttribute;

            if (attr.AttributeClass == null)
            {
                writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>(null);");
                return;
            }

            var fullyQualifiedName = attr.AttributeClass.GloballyQualifiedNonGeneric();

            // Handle data source types that work with properties
            if (fullyQualifiedName == "global::TUnit.Core.ArgumentsAttribute")
            {
                EmitPropertyArgumentsDataSource(writer, propertyName, attr);
            }
            else if (attr.AttributeClass.IsOrInherits("TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
                     attr.AttributeClass.IsOrInherits("TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute"))
            {
                EmitPropertyAsyncDataSource(writer, propertyName, attr);
            }
            else if (fullyQualifiedName == "global::TUnit.Core.MethodDataSourceAttribute")
            {
                EmitPropertyMethodDataSource(writer, propertyName, attr, typeSymbol);
            }
            else if (fullyQualifiedName == "global::TUnit.Core.ClassDataSourceAttribute")
            {
                EmitPropertyClassDataSource(writer, propertyName, attr);
            }
            else
            {
                writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>(null); // Unsupported data source: {fullyQualifiedName}");
            }
        }
        catch (Exception ex)
        {
            writer.AppendLine($"// Error processing property data source for {propData.Property.Name}: {ex.Message}");
            writer.AppendLine($"propertyValues[\"{propData.Property.Name}\"] = () => Task.FromResult<object?>(null);");
        }
    }

    private static void EmitPropertyArgumentsDataSource(CodeWriter writer, string propertyName, AttributeData attr)
    {
        if (attr.ConstructorArguments.Length > 0)
        {
            if (attr.ConstructorArguments[0].Kind == TypedConstantKind.Array &&
                attr.ConstructorArguments[0].Values.Length > 0)
            {
                var value = FormatConstantValue(attr.ConstructorArguments[0].Values[0]);
                writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>({value});");
            }
            else if (attr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
            {
                var value = FormatConstantValue(attr.ConstructorArguments[0]);
                writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>({value});");
            }
        }
        else
        {
            writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>(null);");
        }
    }

    private static void EmitPropertyAsyncDataSource(CodeWriter writer, string propertyName, AttributeData attr)
    {
        writer.AppendLine($"propertyValues[\"{propertyName}\"] = async () => ");
        writer.AppendLine("{");
        writer.Indent();
        
        // For generic data source attributes (e.g. ClassDataSource<T>), the type is in the generic type argument
        if (attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
        {
            var dataSourceType = attr.AttributeClass.TypeArguments[0];
            var fullyQualifiedType = dataSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var safeName = fullyQualifiedType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
            writer.AppendLine($"return await global::TUnit.Core.Generated.DataSourceHelpers.CreateAndInitializeAsync_{safeName}(propertyTestInformation, testSessionId);");
        }
        // For non-generic data source attributes, the type is in the constructor arguments
        else if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is ITypeSymbol dataSourceType)
        {
            var fullyQualifiedType = dataSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var safeName = fullyQualifiedType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
            writer.AppendLine($"return await global::TUnit.Core.Generated.DataSourceHelpers.CreateAndInitializeAsync_{safeName}(propertyTestInformation, testSessionId);");
        }
        else
        {
            writer.AppendLine("throw new InvalidOperationException(\"ClassDataSource attribute does not have a valid type specified. Ensure the attribute has either a generic type argument or a Type constructor argument.\");");
        }
        
        writer.Unindent();
        writer.AppendLine("};");
    }

    private static void EmitPropertyMethodDataSource(CodeWriter writer, string propertyName, AttributeData attr, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine($"propertyValues[\"{propertyName}\"] = async () => ");
        writer.AppendLine("{");
        writer.Indent();
        
        string? methodName = null;
        ITypeSymbol? targetType = null;
        
        // Extract method name and target type from attribute
        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol } _, _
            ])
        {
            targetType = (ITypeSymbol)attr.ConstructorArguments[0].Value;
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            methodName = attr.ConstructorArguments[0].Value?.ToString();
            targetType = typeSymbol;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            writer.AppendLine("throw new InvalidOperationException(\"MethodDataSource attribute does not have a valid method name specified.\");");
        }
        else
        {
            var fullyQualifiedType = targetType?.GloballyQualifiedNonGeneric() ?? typeSymbol.GloballyQualifiedNonGeneric();
            writer.AppendLine($"var data = {fullyQualifiedType}.{methodName}();");
            
            // Use AOT-compatible helper method that handles all the complexity
            writer.AppendLine("return await global::TUnit.Core.Helpers.DataSourceHelpers.ProcessDataSourceResultGeneric(data);");
        }
        
        writer.Unindent();
        writer.AppendLine("};");
    }

    private static void EmitPropertyClassDataSource(CodeWriter writer, string propertyName, AttributeData attr)
    {
        writer.AppendLine($"propertyValues[\"{propertyName}\"] = async () => ");
        writer.AppendLine("{");
        writer.Indent();
        
        // For generic ClassDataSource<T>, the type is in the generic type argument
        if (attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
        {
            var dataSourceType = attr.AttributeClass.TypeArguments[0];
            EmitClassDataSourceInstantiation(writer, dataSourceType, usePropertyTestInformation: true);
        }
        // For non-generic ClassDataSource, the type is in the constructor arguments
        else if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is ITypeSymbol dataSourceType)
        {
            EmitClassDataSourceInstantiation(writer, dataSourceType, usePropertyTestInformation: true);
        }
        else
        {
            writer.AppendLine("throw new InvalidOperationException(\"ClassDataSource attribute on property does not have a valid type specified.\");");
        }
        
        writer.Unindent();
        writer.AppendLine("};");
    }

    private static void EmitClassDataSourceInstantiation(CodeWriter writer, ITypeSymbol dataSourceType, bool usePropertyTestInformation = false)
    {
        var fullyQualifiedType = dataSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeName = fullyQualifiedType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
        var testInfoVar = usePropertyTestInformation ? "propertyTestInformation" : "testInformation";
        writer.AppendLine($"return await global::TUnit.Core.Generated.DataSourceHelpers.CreateAndInitializeAsync_{safeName}({testInfoVar}, testSessionId);");
    }

    private static void EmitPropertyDataSourceGenerator(CodeWriter writer, string propertyName, AttributeData attr, IPropertySymbol property)
    {
        // For now, handle generic data source generators by creating simple instances
        // This is a simplified implementation that can be enhanced later
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is ITypeSymbol targetType)
        {
            var fullyQualifiedType = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var safeName = fullyQualifiedType.Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
            writer.AppendLine($"propertyValues[\"{propertyName}\"] = async () => await global::TUnit.Core.Generated.DataSourceHelpers.CreateAndInitializeAsync_{safeName}(propertyTestInformation, testSessionId);");
        }
        else
        {
            writer.AppendLine($"propertyValues[\"{propertyName}\"] = () => Task.FromResult<object?>(null);");
        }
    }


    private static void EmitAsyncUntypedDataSourceGeneratorAttribute(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// AsyncUntypedDataSourceGeneratorAttribute");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();

        // Create an instance of the generator with all properties from the attribute
        var generatorCode = CodeGenerationHelpers.GenerateAttributeInstantiation(attr);
        writer.AppendLine($"var generator = {generatorCode};");

        writer.AppendLine("// Create TestInformation for the data generator");
        writer.Append("var testInformation = ");
        TestInformationGenerator.GenerateTestInformation(writer, methodSymbol, typeSymbol);
        writer.AppendLine(";");
        writer.AppendLine();
        
        // Initialize nested data source properties if any (after testInformation is created)
        if (attr.AttributeClass != null)
        {
            EmitNestedDataSourceInitialization(writer, attr.AttributeClass, "generator", methodSymbol, typeSymbol);
        }

        writer.AppendLine("// Create MembersToGenerate array based on whether it's class or method level");
        writer.AppendLine("var membersToGenerate = new MemberMetadata[]");
        writer.AppendLine("{");
        writer.Indent();
        if (isClassLevel)
        {
            writer.AppendLine("// For class-level data sources, we need constructor parameters");
            var constructorParams = typeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters;
            if (constructorParams != null && constructorParams.Value.Length > 0)
            {
                foreach (var param in constructorParams.Value)
                {
                    writer.AppendLine($"new ParameterMetadata(typeof({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"Name = \"{param.Name}\",");
                    writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)},");
                    writer.AppendLine("ReflectionInfo = null!");
                    writer.Unindent();
                    writer.AppendLine("},");
                }
            }
        }
        else
        {
            writer.AppendLine("// For method-level data sources, we need method parameters");
            foreach (var param in methodSymbol.Parameters)
            {
                writer.AppendLine($"new ParameterMetadata(typeof({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"Name = \"{param.Name}\",");
                writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)},");
                writer.AppendLine($"ReflectionInfo = typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).GetMethod(\"{methodSymbol.Name}\", new Type[] {{ {string.Join(", ", methodSymbol.Parameters.Select(p => $"typeof({p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"))} }})?.GetParameters().FirstOrDefault(p => p.Name == \"{param.Name}\")!");
                writer.Unindent();
                writer.AppendLine("},");
            }
        }
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        writer.AppendLine("var dataGeneratorMetadata = new DataGeneratorMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"Type = global::TUnit.Core.Enums.DataGeneratorType.{(isClassLevel ? "ClassParameters" : "TestParameters")},");
        writer.AppendLine("TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),");
        writer.AppendLine("MembersToGenerate = membersToGenerate,");
        writer.AppendLine("TestInformation = testInformation,");
        writer.AppendLine("TestSessionId = testSessionId,");
        writer.AppendLine("TestClassInstance = null,");
        writer.AppendLine("ClassInstanceArguments = null");
        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("int classLoopCounter = 0;");
        writer.AppendLine("int methodLoopCounter = 0;");
        writer.AppendLine("await foreach (var dataSourceFunc in generator.GenerateAsync(dataGeneratorMetadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// For SharedType.None, we must not pre-materialize the data");
        writer.AppendLine("// as that would create a single instance that gets shared");
        writer.AppendLine();

        writer.AppendLine("// Get the data and use ToObjectArray to handle both tuples and arrays");
        writer.AppendLine("var initialData = await dataSourceFunc();");
        writer.AppendLine("var dataLength = initialData?.Length ?? 0;");
        writer.AppendLine();
        
        if (isClassLevel)
        {
            writer.AppendLine("// For class data, use ToObjectArray to handle both tuples and arrays");
            writer.AppendLine("var processedData = dataLength == 0 ? new object?[] { null } : ");
            writer.AppendLine("    dataLength == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(initialData![0]) : initialData!;");
            writer.AppendLine();
            writer.AppendLine("var classFactories = processedData.Select((_, index) => new Func<Task<object?>>(async () =>");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var data = await dataSourceFunc();");
            writer.AppendLine("var processed = data?.Length == 0 ? new object?[] { null } :");
            writer.AppendLine("    data?.Length == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(data[0]) : data;");
            writer.AppendLine("if (processed == null || index >= processed.Length) throw new InvalidOperationException($\"Data source index {index} is out of range. Data source returned {processed?.Length ?? 0} items.\");");
            writer.AppendLine("var instance = processed[index];");
            writer.AppendLine("await global::TUnit.Core.Helpers.DataSourceHelpers.InitializeDataSourcePropertiesAsync(instance, testInformation, testSessionId);");
            writer.AppendLine("await global::TUnit.Core.ObjectInitializer.InitializeAsync(instance);");
            writer.AppendLine("return instance;");
            writer.Unindent();
            writer.AppendLine("})).ToArray();");
            writer.AppendLine();
        }

        // Handle method data processing before creating TestDataCombination
        writer.AppendLine("// For method data, process the data using ToObjectArray if needed");
        writer.AppendLine("var processedMethodData = dataLength == 0 ? new object?[] { null } : ");
        writer.AppendLine("    dataLength == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(initialData![0]) : initialData!;");
        writer.AppendLine();

        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        if (isClassLevel)
        {
            writer.AppendLine("ClassDataFactories = classFactories,");
        }
        else
        {
            writer.AppendLine("MethodDataFactories = processedMethodData.Select((_, index) => new Func<Task<object?>>(async () =>");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var data = await dataSourceFunc();");
            writer.AppendLine("var processed = data?.Length == 0 ? new object?[] { null } :");
            writer.AppendLine("    data?.Length == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(data[0]) : data;");
            writer.AppendLine("if (processed == null || index >= processed.Length) throw new InvalidOperationException($\"Data source index {index} is out of range. Data source returned {processed?.Length ?? 0} items.\");");
            writer.AppendLine("return processed[index];");
            writer.Unindent();
            writer.AppendLine("})).ToArray(),");
        }

        // Always write both indices
        writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
        writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

        // Always write both loop indices
        if (isClassLevel)
        {
            writer.AppendLine("ClassLoopIndex = classLoopCounter++,");
            writer.AppendLine("MethodLoopIndex = methodLoopCounter = 0");
        }
        else
        {
            writer.AppendLine("ClassLoopIndex = classLoopCounter,");
            writer.AppendLine("MethodLoopIndex = methodLoopCounter++");
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

    private static void EmitAsyncDataSourceGeneratorAttribute(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// AsyncDataSourceGeneratorAttribute");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();

        // Create an instance of the generator with all properties from the attribute
        var generatorCode = CodeGenerationHelpers.GenerateAttributeInstantiation(attr);
        writer.AppendLine($"var generator = {generatorCode};");

        writer.AppendLine("// Create TestInformation for the data generator");
        writer.Append("var testInformation = ");
        TestInformationGenerator.GenerateTestInformation(writer, methodSymbol, typeSymbol);
        writer.AppendLine(";");
        writer.AppendLine();
        
        // Initialize nested data source properties if any (after testInformation is created)
        if (attr.AttributeClass != null)
        {
            EmitNestedDataSourceInitialization(writer, attr.AttributeClass, "generator", methodSymbol, typeSymbol);
        }

        writer.AppendLine("// Create MembersToGenerate array based on whether it's class or method level");
        writer.AppendLine("var membersToGenerate = new MemberMetadata[]");
        writer.AppendLine("{");
        writer.Indent();
        if (isClassLevel)
        {
            writer.AppendLine("// For class-level data sources, we need constructor parameters");
            var constructorParams = typeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters;
            if (constructorParams != null && constructorParams.Value.Length > 0)
            {
                foreach (var param in constructorParams.Value)
                {
                    writer.AppendLine($"new ParameterMetadata(typeof({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"Name = \"{param.Name}\",");
                    writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)},");
                    writer.AppendLine("ReflectionInfo = null!");
                    writer.Unindent();
                    writer.AppendLine("},");
                }
            }
        }
        else
        {
            writer.AppendLine("// For method-level data sources, we need method parameters");
            foreach (var param in methodSymbol.Parameters)
            {
                writer.AppendLine($"new ParameterMetadata(typeof({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"Name = \"{param.Name}\",");
                writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(param.Type)},");
                writer.AppendLine("ReflectionInfo = null!");
                writer.Unindent();
                writer.AppendLine("},");
            }
        }
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        writer.AppendLine("var dataGeneratorMetadata = new DataGeneratorMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"Type = global::TUnit.Core.Enums.DataGeneratorType.{(isClassLevel ? "ClassParameters" : "TestParameters")},");
        writer.AppendLine("TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),");
        writer.AppendLine("MembersToGenerate = membersToGenerate,");
        writer.AppendLine("TestInformation = testInformation,");
        writer.AppendLine("TestSessionId = testSessionId,");
        writer.AppendLine("TestClassInstance = null,");
        writer.AppendLine("ClassInstanceArguments = null");
        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("int classLoopCounter = 0;");
        writer.AppendLine("int methodLoopCounter = 0;");
        writer.AppendLine("await foreach (var dataSourceFunc in ((IAsyncDataSourceGeneratorAttribute)generator).GenerateAsync(dataGeneratorMetadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// For SharedType.None, we must not pre-materialize the data");
        writer.AppendLine("// as that would create a single instance that gets shared");
        writer.AppendLine();

        writer.AppendLine("// Get the data and use ToObjectArray to handle both tuples and arrays");
        writer.AppendLine("var initialData = await dataSourceFunc();");
        writer.AppendLine("var dataLength = initialData?.Length ?? 0;");
        writer.AppendLine();
        
        if (isClassLevel)
        {
            writer.AppendLine("// For class data, use ToObjectArray to handle both tuples and arrays");
            writer.AppendLine("var processedData = dataLength == 0 ? new object?[] { null } : ");
            writer.AppendLine("    dataLength == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(initialData![0]) : initialData!;");
            writer.AppendLine();
            writer.AppendLine("var classFactories = processedData.Select((_, index) => new Func<Task<object?>>(async () =>");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var data = await dataSourceFunc();");
            writer.AppendLine("var processed = data?.Length == 0 ? new object?[] { null } :");
            writer.AppendLine("    data?.Length == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(data[0]) : data;");
            writer.AppendLine("if (processed == null || index >= processed.Length) throw new InvalidOperationException($\"Data source index {index} is out of range. Data source returned {processed?.Length ?? 0} items.\");");
            writer.AppendLine("var instance = processed[index];");
            writer.AppendLine("await global::TUnit.Core.Helpers.DataSourceHelpers.InitializeDataSourcePropertiesAsync(instance, testInformation, testSessionId);");
            writer.AppendLine("await global::TUnit.Core.ObjectInitializer.InitializeAsync(instance);");
            writer.AppendLine("return instance;");
            writer.Unindent();
            writer.AppendLine("})).ToArray();");
            writer.AppendLine();
        }
        
        // Handle method data processing before creating TestDataCombination
        writer.AppendLine("// For method data, process the data using ToObjectArray if needed");
        writer.AppendLine("var processedMethodData = dataLength == 0 ? new object?[] { null } : ");
        writer.AppendLine("    dataLength == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(initialData![0]) : initialData!;");
        writer.AppendLine();

        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        if (isClassLevel)
        {
            writer.AppendLine("ClassDataFactories = classFactories,");
        }
        else
        {
            writer.AppendLine("MethodDataFactories = processedMethodData.Select((_, index) => new Func<Task<object?>>(async () =>");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var data = await dataSourceFunc();");
            writer.AppendLine("var processed = data?.Length == 0 ? new object?[] { null } :");
            writer.AppendLine("    data?.Length == 1 ? global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(data[0]) : data;");
            writer.AppendLine("if (processed == null || index >= processed.Length) throw new InvalidOperationException($\"Data source index {index} is out of range. Data source returned {processed?.Length ?? 0} items.\");");
            writer.AppendLine("var instance = processed[index];");
            writer.AppendLine("await global::TUnit.Core.Helpers.DataSourceHelpers.InitializeDataSourcePropertiesAsync(instance, testInformation, testSessionId);");
            writer.AppendLine("await global::TUnit.Core.ObjectInitializer.InitializeAsync(instance);");
            writer.AppendLine("return instance;");
            writer.Unindent();
            writer.AppendLine("})).ToArray(),");
        }

        // Always write both indices
        writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
        writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

        // Always write both loop indices
        if (isClassLevel)
        {
            writer.AppendLine("ClassLoopIndex = classLoopCounter++,");
            writer.AppendLine("MethodLoopIndex = methodLoopCounter = 0");
        }
        else
        {
            writer.AppendLine("ClassLoopIndex = classLoopCounter,");
            writer.AppendLine("MethodLoopIndex = methodLoopCounter++");
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

    private static void EmitEmptyCombination(CodeWriter writer, string listName)
    {
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        // Always write both indices
        writer.AppendLine("ClassDataSourceIndex = currentClassIndex,");
        writer.AppendLine("MethodDataSourceIndex = currentMethodIndex,");

        // Always write both loop indices (0 for empty combination)
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");

        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>()");
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

        writer.AppendLine("var mergedProperties = new Dictionary<string, Func<Task<object?>>>();");
        writer.AppendLine("foreach (var kvp in classCombination.PropertyValueFactories) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine("foreach (var kvp in methodCombination.PropertyValueFactories) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine("foreach (var kvp in propertyCombination.PropertyValueFactories) mergedProperties[kvp.Key] = kvp.Value;");
        writer.AppendLine();

        writer.AppendLine("allCombinations.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("ClassDataFactories = classCombination.ClassDataFactories ?? Array.Empty<Func<Task<object?>>>(),");
        writer.AppendLine("MethodDataFactories = methodCombination.MethodDataFactories ?? Array.Empty<Func<Task<object?>>>(),");
        writer.AppendLine("ClassDataSourceIndex = classCombination.ClassDataSourceIndex,");
        writer.AppendLine("ClassLoopIndex = classCombination.ClassLoopIndex,");
        writer.AppendLine("MethodDataSourceIndex = methodCombination.MethodDataSourceIndex,");
        writer.AppendLine("MethodLoopIndex = methodCombination.MethodLoopIndex,");
        writer.AppendLine("PropertyValueFactories = mergedProperties,");
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
        var allAttributes = symbol.GetAttributes();
        var dataSourceAttributes = allAttributes
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToImmutableArray();
            
        // Debug logging - this will be generated into the test code
        // We need to emit this as generated code, not compile-time console output
        return dataSourceAttributes;
    }

    private static ImmutableArray<PropertyWithDataSource> GetPropertyDataSources(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<PropertyWithDataSource>();

        // Walk inheritance hierarchy to include base class properties
        var currentType = typeSymbol;
        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false } property) // Only instance properties for test data combinations
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        // Check if we already have this property (in case of overrides)
                        if (!properties.Any(p => p.Property.Name == property.Name))
                        {
                            properties.Add(new PropertyWithDataSource
                            {
                                Property = property,
                                DataSourceAttribute = dataSourceAttr
                            });
                        }
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        return properties.ToImmutableArray();
    }

    private static ImmutableArray<PropertyWithDataSource> GetStaticPropertyDataSources(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<PropertyWithDataSource>();

        // Walk inheritance hierarchy to include base class static properties
        var currentType = typeSymbol;
        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: true } property) // Only static properties for session initialization
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        // Check if we already have this property (in case of overrides)
                        if (!properties.Any(p => p.Property.Name == property.Name))
                        {
                            properties.Add(new PropertyWithDataSource
                            {
                                Property = property,
                                DataSourceAttribute = dataSourceAttr
                            });
                        }
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        return properties.ToImmutableArray();
    }

    private static bool IsAsyncDataSourceGeneratorAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        // Check if it's AsyncDataSourceGeneratorAttribute or inherits from it
        return attributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute");
    }

    private static bool IsAsyncUntypedDataSourceGeneratorAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        return attributeClass.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute");
    }

    private static bool IsDataSourceGeneratorAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        // Check if it's DataSourceGeneratorAttribute or inherits from it
        return attributeClass.IsOrInherits("global::TUnit.Core.DataSourceGeneratorAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.UntypedDataSourceGeneratorAttribute");
    }
    
    private static bool IsOrInheritsGeneric(INamedTypeSymbol attributeClass, string baseTypeName)
    {
        // Check if the current type matches (for generic types, compare without type arguments)
        var currentType = attributeClass;
        while (currentType != null)
        {
            // Get the fully qualified name without generic arguments
            var typeName = currentType.IsGenericType 
                ? currentType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
            // For generic types, compare the base name without type parameters
            if (typeName.StartsWith(baseTypeName + "<") || typeName == baseTypeName)
            {
                return currentType.IsGenericType;
            }
            
            currentType = currentType.BaseType;
        }
        
        return false;
    }
    
    private static INamedTypeSymbol? GetGenericAsyncDataSourceBase(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return null;
        
        var currentType = attributeClass;
        while (currentType != null)
        {
            // Check if this is a constructed generic type that matches our base
            if (currentType is { IsGenericType: true, ConstructedFrom: not null })
            {
                var typeName = currentType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (typeName.StartsWith("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
                    typeName.StartsWith("global::TUnit.Core.DataSourceGeneratorAttribute"))
                {
                    return currentType;
                }
            }
            
            currentType = currentType.BaseType;
        }
        
        return null;
    }


    private static readonly TypedConstantFormatter _formatter = new();

    public static string FormatConstantValue(TypedConstant constant)
    {
        try
        {
            // Use the formatter for consistent handling
            return _formatter.FormatForCode(constant);
        }
        catch
        {
            // Fallback to simple string representation
            return constant.Value?.ToString() ?? "null";
        }
    }

    private static string FormatConstantValueWithType(TypedConstant constant, ITypeSymbol? targetType)
    {
        try
        {
            // Use the formatter with target type for proper conversions
            return _formatter.FormatForCode(constant, targetType);
        }
        catch
        {
            // Fallback to simple string representation
            return constant.Value?.ToString() ?? "null";
        }
    }

    private static string FormatConstantValueOld(TypedConstant constant)
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

            if (constant is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
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

            if (constant.Value is byte byteValue)
            {
                return $"(byte){byteValue}";
            }

            if (constant.Value is sbyte sbyteValue)
            {
                return $"(sbyte){sbyteValue}";
            }

            if (constant.Value is short shortValue)
            {
                return $"(short){shortValue}";
            }

            if (constant.Value is ushort ushortValue)
            {
                return $"(ushort){ushortValue}";
            }

            return constant.Value?.ToString() ?? "null";
        }
        catch
        {
            return "null";
        }
    }

    private static int GetRepeatCount(IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        // Check method first, then class, then assembly
        var repeatAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.RepeatAttribute");

        if (repeatAttr == null)
        {
            repeatAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.RepeatAttribute");
        }

        if (repeatAttr == null)
        {
            repeatAttr = typeSymbol.ContainingAssembly.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.RepeatAttribute");
        }

        if (repeatAttr?.ConstructorArguments.Length > 0 &&
            repeatAttr.ConstructorArguments[0].Value is int repeatTimes and > 0)
        {
            return repeatTimes;
        }

        return 0; // Default: no repeat
    }


    private static bool IsAsyncEnumerable(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check if it implements IAsyncEnumerable<T>
        var asyncEnumerableInterface = namedType.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>");

        if (asyncEnumerableInterface != null)
        {
            return true;
        }

        // Check if the type itself is IAsyncEnumerable<T>
        if (namedType.IsGenericType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>")
        {
            return true;
        }

        return false;
    }

    private static bool IsEnumerable(ITypeSymbol typeSymbol)
    {
        // Arrays are enumerable
        if (typeSymbol is IArrayTypeSymbol)
        {
            return true;
        }

        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check if it implements IEnumerable<T>
        var enumerableInterface = namedType.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");

        if (enumerableInterface != null)
        {
            return true;
        }

        // Check if the type itself is IEnumerable<T>
        if (namedType.IsGenericType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
        {
            return true;
        }

        // Check for non-generic IEnumerable
        var nonGenericEnumerable = namedType.AllInterfaces
            .FirstOrDefault(i => i.ToDisplayString() == "System.Collections.IEnumerable");

        return nonGenericEnumerable != null;
    }
    
    private static bool IsFuncType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }
        
        // Check if it's a Func<T> delegate type
        if (namedType is { IsGenericType: true, TypeKind: TypeKind.Delegate, Name: "Func" } &&
            namedType.ContainingNamespace.ToDisplayString() == "System")
        {
            return true;
        }
        
        return false;
    }
    
    private static void EmitNestedDataSourceInitialization(CodeWriter writer, INamedTypeSymbol typeSymbol, string instanceName, IMethodSymbol methodSymbol, INamedTypeSymbol containingTypeSymbol)
    {
        EmitNestedDataSourceInitializationRecursive(writer, typeSymbol, instanceName, 0, methodSymbol, containingTypeSymbol);
    }
    
    private static void EmitInstanceDataSourcePropertyInitialization(CodeWriter writer, string instanceVarName, string typeVarName, IMethodSymbol methodSymbol, INamedTypeSymbol containingTypeSymbol)
    {
        writer.AppendLine("// Initialize data source properties on the instance");
        writer.AppendLine($"var instanceType_{instanceVarName} = {instanceVarName}?.GetType();");
        writer.AppendLine($"if (instanceType_{instanceVarName} != null)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"await global::TUnit.Core.Helpers.DataSourceHelpers.InitializeDataSourcePropertiesAsync({instanceVarName}, testInformation, testSessionId);");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }
    
    // Instance data source property initialization is now handled by generated type-specific helpers
    // registered at runtime and called via DataSourceHelpers.InitializeDataSourcePropertiesAsync
    
    private static void EmitNestedDataSourceInitializationRecursive(CodeWriter writer, INamedTypeSymbol typeSymbol, string instanceName, int depth, IMethodSymbol methodSymbol, INamedTypeSymbol containingTypeSymbol)
    {
        var dataSourceProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.SetMethod != null
                && p.GetAttributes().Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass)))
            .ToList();
            
        if (!dataSourceProperties.Any())
        {
            return;
        }
        
        writer.AppendLine();
        writer.AppendLine("// Initialize nested data source properties");
        
        // testInformation should already be defined in the parent scope when this is called
        
        var propertyIndex = 0;
        foreach (var property in dataSourceProperties)
        {
            var dataSourceAttr = property.GetAttributes()
                .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));
                
            if (dataSourceAttr?.AttributeClass == null)
            {
                continue;
            }
            
            var propertyName = property.Name;
            var varName = $"nested_{depth}_{propertyIndex}_{propertyName}";
            propertyIndex++;
            
            // All data source attributes need to be processed to generate values
            writer.AppendLine($"// Initialize property {propertyName} from data source");
            writer.AppendLine("{");
            writer.Indent();
            
            var generatorCode = CodeGenerationHelpers.GenerateAttributeInstantiation(dataSourceAttr);
            writer.AppendLine($"var dataSourceGenerator_{varName} = {generatorCode};");
            
            // Recursively initialize the generator's properties if it has any data source properties
            if (dataSourceAttr.AttributeClass.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.DeclaredAccessibility == Accessibility.Public && p.SetMethod != null
                    && p.GetAttributes().Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))))
            {
                EmitNestedDataSourceInitializationRecursive(writer, dataSourceAttr.AttributeClass, $"dataSourceGenerator_{varName}", depth + 1, methodSymbol, containingTypeSymbol);
            }
            
            // Create metadata for the generator
            writer.AppendLine($"var metadata_{varName} = new global::TUnit.Core.DataGeneratorMetadata");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("Type = global::TUnit.Core.Enums.DataGeneratorType.Property,");
            writer.AppendLine("TestBuilderContext = new global::TUnit.Core.TestBuilderContextAccessor(new global::TUnit.Core.TestBuilderContext()),");
            writer.AppendLine("MembersToGenerate = new global::TUnit.Core.MemberMetadata[0],");
            writer.AppendLine("TestInformation = testInformation,");
            writer.AppendLine("TestSessionId = testSessionId,");
            writer.AppendLine("TestClassInstance = null,");
            writer.AppendLine("ClassInstanceArguments = null");
            writer.Unindent();
            writer.AppendLine("};");
            
            // Check if it's IAsyncDataSourceGeneratorAttribute (all async data sources implement this public interface)
            writer.AppendLine($"if (dataSourceGenerator_{varName} is global::TUnit.Core.IAsyncDataSourceGeneratorAttribute asyncGenerator_{varName})");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"await foreach (var dataSourceFunc in asyncGenerator_{varName}.GenerateAsync(metadata_{varName}))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var data = await dataSourceFunc();");
            writer.AppendLine("if (data?.Length > 0)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var instance = data[0];");
            writer.AppendLine("await global::TUnit.Core.ObjectInitializer.InitializeAsync(instance);");
            writer.AppendLine($"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).GetProperty(\"{propertyName}\")!.SetValue({instanceName}, ({property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})instance);");
            writer.AppendLine("break;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            
            writer.Unindent();
            writer.AppendLine("}");
        }
        
        // Finally, initialize the instance itself if it implements IAsyncInitializer
        writer.AppendLine($"await global::TUnit.Core.ObjectInitializer.InitializeAsync({instanceName});");
        writer.AppendLine();
    }

    /// <summary>
    /// Gets the target type for a given argument index, handling params parameters correctly.
    /// For params parameters, returns the element type of the array instead of the array type itself.
    /// </summary>
    private static ITypeSymbol? GetTargetTypeForArgument(ImmutableArray<IParameterSymbol> parameters, int argumentIndex)
    {
        if (argumentIndex >= parameters.Length)
        {
            return null;
        }

        var parameter = parameters[argumentIndex];
        
        // If this is the last parameter and it's a params parameter
        if (argumentIndex == parameters.Length - 1 && parameter is { IsParams: true, Type: IArrayTypeSymbol arrayType })
            // For params parameters, we need to use the element type of the array
        {
            return arrayType.ElementType;
        }
        
        // For regular parameters, return the parameter type
        return parameter.Type;
    }

    private static List<string> ProcessArgumentsForParams(ImmutableArray<TypedConstant> arguments, ImmutableArray<IParameterSymbol> parameters)
    {
        var formattedArgs = new List<string>();
        
        if (parameters.IsEmpty)
        {
            // No parameters, just format arguments as-is
            for (var i = 0; i < arguments.Length; i++)
            {
                formattedArgs.Add(FormatConstantValueWithType(arguments[i], null));
            }
            return formattedArgs;
        }

        // Process regular parameters
        var regularParameterCount = parameters.Length;
        var lastParameter = parameters.LastOrDefault();
        
        if (lastParameter?.IsParams == true)
        {
            regularParameterCount--; // Last parameter is params, so reduce regular count
        }

        // Process regular parameters
        for (var i = 0; i < regularParameterCount && i < arguments.Length; i++)
        {
            var targetType = parameters[i].Type;
            formattedArgs.Add(FormatConstantValueWithType(arguments[i], targetType));
        }

        // Process params parameter if it exists
        if (lastParameter?.IsParams == true && regularParameterCount < arguments.Length)
        {
            var paramsElementType = (lastParameter.Type as IArrayTypeSymbol)?.ElementType;
            var remainingArgs = arguments.Skip(regularParameterCount).ToArray();
            
            // Create an array literal for the params parameter
            var paramsElements = remainingArgs.Select(arg => FormatConstantValueWithType(arg, paramsElementType));
            var paramsArrayLiteral = $"new {paramsElementType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object"}[] {{ {string.Join(", ", paramsElements)} }}";
            formattedArgs.Add(paramsArrayLiteral);
        }
        else if (lastParameter?.IsParams == true)
        {
            // No arguments for params parameter, create empty array
            var paramsElementType = (lastParameter.Type as IArrayTypeSymbol)?.ElementType;
            var paramsArrayLiteral = $"new {paramsElementType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object"}[0]";
            formattedArgs.Add(paramsArrayLiteral);
        }

        return formattedArgs;
    }

    private static void EmitGenericTypeResolution(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        ImmutableArray<AttributeData> methodDataSources,
        ImmutableArray<AttributeData> classDataSources,
        ImmutableArray<PropertyWithDataSource> propertyDataSources,
        int repeatCount,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine("// Generic type resolution from data sources");
        writer.AppendLine("// Extract data values and infer generic type arguments");
        writer.AppendLine();
        
        // Check for various data source types that can provide type information
        var argumentsAttributes = methodDataSources.Where(attr => 
            attr.AttributeClass?.Name == "ArgumentsAttribute").ToArray();
        
        var typedAsyncDataSources = methodDataSources.Where(attr =>
            attr.AttributeClass != null && 
            IsOrInheritsGeneric(attr.AttributeClass, "global::TUnit.Core.AsyncDataSourceGeneratorAttribute")).ToArray();
        
        // Check for untyped data sources that might read from typed parameter attributes
        var untypedDataSources = methodDataSources.Where(attr =>
            attr.AttributeClass != null &&
            (attr.AttributeClass.IsOrInherits("global::TUnit.Core.UntypedDataSourceGeneratorAttribute") ||
             attr.AttributeClass.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute"))).ToArray();
        
        writer.AppendLine($"// Debug: Found {argumentsAttributes.Length} Arguments attributes at compile time");
        writer.AppendLine($"// Debug: Found {typedAsyncDataSources.Length} typed data sources (async/sync) at compile time");
        writer.AppendLine($"// Debug: Found {untypedDataSources.Length} untyped data sources at compile time");
        // Method data sources count: {methodDataSources.Length}
        
        if (argumentsAttributes.Any() || typedAsyncDataSources.Any() || untypedDataSources.Any())
        {
            writer.AppendLine("// Process data sources for generic type resolution");
            writer.AppendLine("var genericCombinations = new List<TestDataCombination>();");
            writer.AppendLine();
            
            // Handle Arguments attributes
            if (argumentsAttributes.Any())
            {
                writer.AppendLine("// Process Arguments attributes");
                EmitArgumentsBasedGenericResolution(writer, methodSymbol, typeSymbol, argumentsAttributes, testMethodMetadata);
            }
            
            // Handle typed data sources (both async and sync inherit from AsyncDataSourceGeneratorAttribute)
            if (typedAsyncDataSources.Any())
            {
                writer.AppendLine("// Process typed data sources");
                EmitTypedDataSourceGenericResolution(writer, methodSymbol, typeSymbol, typedAsyncDataSources, testMethodMetadata);
            }
            
            // Handle untyped data sources by checking parameter attributes
            if (untypedDataSources.Any())
            {
                writer.AppendLine("// Process untyped data sources with parameter type inference");
                EmitParameterBasedGenericResolution(writer, methodSymbol, typeSymbol, untypedDataSources, testMethodMetadata);
            }
            
            // Apply repeat count
            writer.AppendLine();
            writer.AppendLine("// Apply repeat count to generic combinations");
            writer.AppendLine($"for (var repeatIndex = 0; repeatIndex <= {repeatCount}; repeatIndex++)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var combination in genericCombinations)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("ClassDataFactories = combination.ClassDataFactories,");
            writer.AppendLine("MethodDataFactories = combination.MethodDataFactories,");
            writer.AppendLine("ClassDataSourceIndex = combination.ClassDataSourceIndex,");
            writer.AppendLine("MethodDataSourceIndex = combination.MethodDataSourceIndex,");
            writer.AppendLine("ClassLoopIndex = combination.ClassLoopIndex,");
            writer.AppendLine("MethodLoopIndex = combination.MethodLoopIndex,");
            writer.AppendLine("PropertyValueFactories = combination.PropertyValueFactories,");
            writer.AppendLine("DataGenerationException = combination.DataGenerationException,");
            writer.AppendLine("DisplayName = combination.DisplayName,");
            writer.AppendLine("RepeatIndex = repeatIndex,");
            writer.AppendLine("ResolvedGenericTypes = combination.ResolvedGenericTypes");
            writer.Unindent();
            writer.AppendLine("};");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            writer.AppendLine("// No typed data sources found - cannot infer types");
            writer.AppendLine("yield return new TestDataCombination");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("DataGenerationException = new System.InvalidOperationException(");
            writer.AppendLine("    \"Generic tests require typed data sources for type inference. \" +");
            writer.AppendLine("    \"Use [Arguments] or typed data sources that inherit from AsyncDataSourceGeneratorAttribute<T>.\"),");
            writer.AppendLine("DisplayName = \"[GENERIC TYPE INFERENCE FAILED: No typed data sources]\"");
            writer.Unindent();
            writer.AppendLine("};");
        }
    }

    private static void EmitArgumentsBasedGenericResolution(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        AttributeData[] argumentsAttributes,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine("// Process each Arguments attribute to infer types");
        
        foreach (var attr in argumentsAttributes)
        {
            if (attr.ConstructorArguments.Length == 0)
            {
                continue;
            }
            
            writer.AppendLine("// Process Arguments attribute");
            writer.AppendLine("{");
            writer.Indent();
            
            // Add debug logging
            // Processing Arguments attribute with {attr.ConstructorArguments.Length} constructor arguments
            
            // Generate the argument values
            writer.AppendLine("var argumentValues = new object?[]");
            writer.AppendLine("{");
            writer.Indent();
            
            // For Arguments attribute, the constructor arguments are in a params array
            // So we need to extract the individual values from the array
            if (attr.ConstructorArguments.Length > 0)
            {
                var firstArg = attr.ConstructorArguments[0];
                if (firstArg.Kind == TypedConstantKind.Array)
                {
                    // Extract individual values from the array
                    foreach (var value in firstArg.Values)
                    {
                        var formattedValue = FormatConstantValue(value);
                        writer.AppendLine($"{formattedValue},");
                    }
                }
                else
                {
                    // Single value
                    var formattedValue = FormatConstantValue(firstArg);
                    writer.AppendLine($"{formattedValue},");
                }
            }
            
            writer.Unindent();
            writer.AppendLine("};");
            writer.AppendLine();
            
            // Infer types from argument values
            writer.AppendLine("// Infer generic type arguments from argument values");
            writer.AppendLine("var inferredTypes = new System.Type[argumentValues.Length];");
            writer.AppendLine("for (int i = 0; i < argumentValues.Length; i++)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("inferredTypes[i] = argumentValues[i]?.GetType() ?? typeof(object);");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
            
            // Generate type parameter mapping
            EmitTypeParameterMapping(writer, methodSymbol, typeSymbol, testMethodMetadata);
            
            if (testMethodMetadata?.IsGenericType == true)
            {
                EmitGenericTypeInstantiationToList(writer, methodSymbol, typeSymbol, testMethodMetadata);
            }
            else if (testMethodMetadata?.IsGenericMethod == true)
            {
                EmitGenericMethodInstantiationToList(writer, methodSymbol, typeSymbol, testMethodMetadata);
            }
            
            writer.Unindent();
            writer.AppendLine("}");
        }
    }

    private static void EmitTypeParameterMapping(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine("// Map inferred types to generic type parameters");
        writer.AppendLine("var resolvedGenericTypes = new Dictionary<string, System.Type>();");
        writer.AppendLine();
        
        if (testMethodMetadata?.IsGenericType == true)
        {
            // Handle generic class types
            var genericParameters = typeSymbol.TypeParameters;
            writer.AppendLine("// Generic class type parameters");
            for (int i = 0; i < genericParameters.Length; i++)
            {
                var paramName = genericParameters[i].Name;
                writer.AppendLine($"if (inferredTypes.Length > {i})");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"resolvedGenericTypes[\"{paramName}\"] = inferredTypes[{i}];");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"resolvedGenericTypes[\"{paramName}\"] = typeof(object); // Fallback");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Validate constraints for class type parameters
            writer.AppendLine();
            writer.AppendLine("// Create type array from resolved types for constraint validation");
            writer.AppendLine($"var classTypeArgs = new Type[{genericParameters.Length}];");
            for (int i = 0; i < genericParameters.Length; i++)
            {
                var paramName = genericParameters[i].Name;
                writer.AppendLine($"classTypeArgs[{i}] = resolvedGenericTypes[\"{paramName}\"];");
            }
            EmitGenericConstraintValidationForInferredTypes(writer, genericParameters, "class");
        }
        
        if (testMethodMetadata?.IsGenericMethod == true)
        {
            // Handle generic method type parameters
            var methodParameters = methodSymbol.TypeParameters;
            writer.AppendLine("// Generic method type parameters");
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var paramName = methodParameters[i].Name;
                writer.AppendLine($"if (inferredTypes.Length > {i})");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"resolvedGenericTypes[\"{paramName}\"] = inferredTypes[{i}];");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"resolvedGenericTypes[\"{paramName}\"] = typeof(object); // Fallback");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Validate constraints for method type parameters
            writer.AppendLine();
            writer.AppendLine("// Create type array from resolved types for constraint validation");
            writer.AppendLine($"var methodTypeArgs = new Type[{methodParameters.Length}];");
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var paramName = methodParameters[i].Name;
                writer.AppendLine($"methodTypeArgs[{i}] = resolvedGenericTypes[\"{paramName}\"];");
            }
            EmitGenericConstraintValidationForInferredTypes(writer, methodParameters, "method");
        }
        
        writer.AppendLine();
    }

    private static void EmitGenericTypeInstantiation(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        TestMethodMetadata testMethodMetadata)
    {
        writer.AppendLine("// Create concrete generic type instance");
        writer.AppendLine("TestDataCombination combination = null;");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// Create TestDataCombination with resolved type information");
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("MethodDataFactories = argumentValues.Select((arg, index) => new Func<Task<object?>>(async () => arg)).ToArray(),");
        writer.AppendLine("DisplayName = $\"TestWithValue({TUnit.Core.Helpers.ArgumentFormatter.FormatArguments(argumentValues)})\",");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        
        // Add resolved generic type information
        writer.AppendLine("// Store resolved generic type information");
        writer.AppendLine("ResolvedGenericTypes = resolvedGenericTypes");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine("DisplayName = $\"[GENERIC TYPE RESOLUTION ERROR: {ex.Message}]\"");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        // Yielding generic test combination
        writer.AppendLine("yield return combination;");
    }
    
    private static void EmitGenericTypeInstantiationToList(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        TestMethodMetadata testMethodMetadata)
    {
        writer.AppendLine("// Create concrete generic type instance");
        writer.AppendLine("TestDataCombination combination = null;");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// Create TestDataCombination with resolved type information");
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("MethodDataFactories = argumentValues.Select((arg, index) => new Func<Task<object?>>(async () => arg)).ToArray(),");
        writer.AppendLine("DisplayName = $\"TestWithValue({TUnit.Core.Helpers.ArgumentFormatter.FormatArguments(argumentValues)})\",");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        
        // Add resolved generic type information
        writer.AppendLine("// Store resolved generic type information");
        writer.AppendLine("ResolvedGenericTypes = resolvedGenericTypes");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine("DisplayName = $\"[GENERIC TYPE RESOLUTION ERROR: {ex.Message}]\"");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("genericCombinations.Add(combination);");
    }

    private static void EmitGenericMethodInstantiation(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        TestMethodMetadata testMethodMetadata)
    {
        writer.AppendLine("// Create concrete generic method instance");
        writer.AppendLine("TestDataCombination combination = null;");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("MethodDataFactories = argumentValues.Select((arg, index) => new Func<Task<object?>>(async () => arg)).ToArray(),");
        writer.AppendLine("DisplayName = $\"GenericMethod<{string.Join(\", \", resolvedGenericTypes.Values.Select(t => t.Name))}>({TUnit.Core.Helpers.ArgumentFormatter.FormatArguments(argumentValues)})\",");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        
        // Add resolved generic type information
        writer.AppendLine("ResolvedGenericTypes = resolvedGenericTypes");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine("DisplayName = $\"[GENERIC METHOD RESOLUTION ERROR: {ex.Message}]\"");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        // Yielding generic test combination
        writer.AppendLine("yield return combination;");
    }
    
    private static void EmitTypedDataSourceGenericResolution(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        AttributeData[] typedDataSources,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine($"// Processing {typedDataSources.Length} typed data sources");
        
        foreach (var attr in typedDataSources)
        {
            // Find the generic base class to get type arguments
            var genericBase = GetGenericAsyncDataSourceBase(attr.AttributeClass);
            if (genericBase?.TypeArguments.Length > 0)
            {
                writer.AppendLine("// Extract type arguments from data source attribute base class");
                writer.AppendLine("var typedDataSourceTypes = new Dictionary<string, Type>();");
                
                // Get the type arguments from the generic base
                var attributeTypeArgs = genericBase.TypeArguments;
                
                // Map to generic parameters based on position
                if (methodSymbol.IsGenericMethod)
                {
                    // For generic methods, map to method type parameters
                    for (int i = 0; i < Math.Min(attributeTypeArgs.Length, methodSymbol.TypeParameters.Length); i++)
                    {
                        var typeParam = methodSymbol.TypeParameters[i];
                        var typeArg = attributeTypeArgs[i];
                        writer.AppendLine($"typedDataSourceTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                    }
                    
                    // Validate constraints for method type parameters
                    var typeArgStrings = attributeTypeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    EmitGenericConstraintValidation(writer, methodSymbol.TypeParameters, typeArgStrings, "method");
                }
                else if (typeSymbol.IsGenericType)
                {
                    // For generic classes, map to class type parameters
                    for (int i = 0; i < Math.Min(attributeTypeArgs.Length, typeSymbol.TypeParameters.Length); i++)
                    {
                        var typeParam = typeSymbol.TypeParameters[i];
                        var typeArg = attributeTypeArgs[i];
                        writer.AppendLine($"typedDataSourceTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                    }
                    
                    // Validate constraints for class type parameters
                    var typeArgStrings = attributeTypeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    EmitGenericConstraintValidation(writer, typeSymbol.TypeParameters, typeArgStrings, "class");
                }
                
                // Generate the data combination
                writer.AppendLine("// Create test data combination with resolved types");
                writer.AppendLine("genericCombinations.Add(new TestDataCombination");
                writer.AppendLine("{");
                writer.Indent();
                
                // Generate display name
                var displayNameParts = new List<string>();
                if (methodSymbol.IsGenericMethod)
                {
                    var typeNames = string.Join(", ", attributeTypeArgs.Select(t => t.Name));
                    displayNameParts.Add($"{methodSymbol.Name}<{typeNames}>");
                }
                else if (typeSymbol.IsGenericType)
                {
                    var typeNames = string.Join(", ", attributeTypeArgs.Select(t => t.Name));
                    displayNameParts.Add($"{typeSymbol.Name}<{typeNames}>");
                }
                
                writer.AppendLine($"DisplayName = \"{string.Join(" - ", displayNameParts)}\",");
                writer.AppendLine("ResolvedGenericTypes = typedDataSourceTypes,");
                writer.AppendLine("ClassDataSourceIndex = 0,");
                writer.AppendLine("MethodDataSourceIndex = 0,");
                writer.AppendLine("ClassLoopIndex = 0,");
                writer.AppendLine("MethodLoopIndex = 0,");
                writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
                
                // Store the data source type for runtime generation
                var attributeFullName = attr.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                writer.AppendLine($"// Store typed data source info for runtime generation: {attributeFullName}");
                writer.AppendLine("MethodDataFactories = new Func<Task<object?>>[]");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("// This will be replaced by runtime data source generation");
                writer.AppendLine("async () => await global::TUnit.Core.TestDataSourceGenerator.GenerateTypedDataSourceValueAsync(");
                writer.AppendLine($"    typeof({attributeFullName}), testSessionId, typedDataSourceTypes)");
                writer.Unindent();
                writer.AppendLine("},");
                
                writer.Unindent();
                writer.AppendLine("});");
            }
        }
    }
    
    private static void EmitGenericMethodInstantiationToList(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        TestMethodMetadata testMethodMetadata)
    {
        writer.AppendLine("// Create concrete generic method instance");
        writer.AppendLine("TestDataCombination combination = null;");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("MethodDataFactories = argumentValues.Select((arg, index) => new Func<Task<object?>>(async () => arg)).ToArray(),");
        writer.AppendLine("DisplayName = $\"GenericMethod<{string.Join(\", \", resolvedGenericTypes.Values.Select(t => t.Name))}>({TUnit.Core.Helpers.ArgumentFormatter.FormatArguments(argumentValues)})\",");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        
        // Add resolved generic type information
        writer.AppendLine("ResolvedGenericTypes = resolvedGenericTypes");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("combination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = ex,");
        writer.AppendLine("DisplayName = $\"[GENERIC METHOD RESOLUTION ERROR: {ex.Message}]\"");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("genericCombinations.Add(combination);");
    }
    
    private static void EmitGenericConstraintValidationForInferredTypes(
        CodeWriter writer,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        string constraintContext)
    {
        writer.AppendLine($"// Validate generic constraints for {constraintContext}");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var constraintErrors = new List<string>();");
        
        for (int i = 0; i < typeParameters.Length; i++)
        {
            var typeParam = typeParameters[i];
            var typeArrayName = constraintContext == "class" ? "classTypeArgs" : "methodTypeArgs";
            
            writer.AppendLine($"// Check constraints for {typeParam.Name}");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"var type = {typeArrayName}[{i}];");
            
            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                writer.AppendLine($"if (type.IsValueType)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a class constraint but '{{type.Name}}' is a value type\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                writer.AppendLine($"if (!type.IsValueType)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a struct constraint but '{{type.Name}}' is not a value type\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check new() constraint
            if (typeParam.HasConstructorConstraint)
            {
                writer.AppendLine($"if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a new() constraint but '{{type.Name}}' does not have a parameterless constructor\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check base type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                if (constraintType.TypeKind == TypeKind.Interface)
                {
                    var interfaceName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"if (!typeof({interfaceName}).IsAssignableFrom(type))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' must implement '{interfaceName}' but '{{type.Name}}' does not\");");
                    writer.Unindent();
                    writer.AppendLine("}");
                }
                else if (constraintType.TypeKind == TypeKind.Class)
                {
                    var baseClassName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"if (!typeof({baseClassName}).IsAssignableFrom(type))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' must derive from '{baseClassName}' but '{{type.Name}}' does not\");");
                    writer.Unindent();
                    writer.AppendLine("}");
                }
            }
            
            writer.Unindent();
            writer.AppendLine("}");
        }
        
        writer.AppendLine("if (constraintErrors.Any())");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("throw new InvalidOperationException($\"Generic constraint validation failed:\\n{string.Join(\"\\n\", constraintErrors)}\");");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }
    
    private static void EmitParameterBasedGenericResolution(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol,
        AttributeData[] untypedDataSources,
        TestMethodMetadata? testMethodMetadata)
    {
        writer.AppendLine($"// Checking {methodSymbol.Parameters.Length} parameters for typed attributes");
        
        // Look for typed attributes on parameters
        writer.AppendLine("// Extract types from parameter attributes");
        writer.AppendLine("var parameterBasedTypes = new Dictionary<string, Type>();");
        writer.AppendLine("var foundTypedParameterAttribute = false;");
        writer.AppendLine();
        
        // Check each parameter for typed attributes
        for (int paramIndex = 0; paramIndex < methodSymbol.Parameters.Length; paramIndex++)
        {
            var parameter = methodSymbol.Parameters[paramIndex];
            writer.AppendLine($"// Check parameter '{parameter.Name}'");
            
            foreach (var attr in parameter.GetAttributes())
            {
                // Check if this is a generic attribute (e.g., Matrix<T>)
                if (attr.AttributeClass?.IsGenericType == true)
                {
                    var baseTypeName = attr.AttributeClass.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"// Found generic attribute: {baseTypeName}");
                    
                    // Extract type arguments from the attribute
                    var typeArgs = attr.AttributeClass.TypeArguments;
                    if (typeArgs.Length > 0)
                    {
                        writer.AppendLine("foundTypedParameterAttribute = true;");
                        
                        // Map type arguments to generic parameters
                        if (methodSymbol.IsGenericMethod)
                        {
                            // For generic methods, try to map based on parameter position
                            if (paramIndex < methodSymbol.TypeParameters.Length)
                            {
                                var typeParam = methodSymbol.TypeParameters[paramIndex];
                                var typeArg = typeArgs[0]; // Take first type argument from attribute
                                writer.AppendLine($"parameterBasedTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                            }
                        }
                        else if (typeSymbol.IsGenericType)
                        {
                            // For generic classes, similar mapping
                            if (paramIndex < typeSymbol.TypeParameters.Length)
                            {
                                var typeParam = typeSymbol.TypeParameters[paramIndex];
                                var typeArg = typeArgs[0];
                                writer.AppendLine($"parameterBasedTypes[\"{typeParam.Name}\"] = typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)});");
                            }
                        }
                    }
                }
            }
        }
        
        writer.AppendLine();
        writer.AppendLine("if (foundTypedParameterAttribute)");
        writer.AppendLine("{");
        writer.Indent();
        
        // Validate constraints if we found types
        if (methodSymbol is { IsGenericMethod: true, TypeParameters.Length: > 0 })
        {
            writer.AppendLine("// Validate constraints for method type parameters");
            writer.AppendLine($"var typeArray = new Type[{methodSymbol.TypeParameters.Length}];");
            for (int i = 0; i < methodSymbol.TypeParameters.Length; i++)
            {
                var typeParam = methodSymbol.TypeParameters[i];
                writer.AppendLine($"typeArray[{i}] = parameterBasedTypes.TryGetValue(\"{typeParam.Name}\", out var t{i}) ? t{i} : typeof(object);");
            }
            EmitGenericConstraintValidation(writer, methodSymbol.TypeParameters, 
                methodSymbol.TypeParameters.Select(tp => $"typeArray[{methodSymbol.TypeParameters.IndexOf(tp)}]").ToArray(), "method");
        }
        else if (typeSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            writer.AppendLine("// Validate constraints for class type parameters");
            writer.AppendLine($"var typeArray = new Type[{typeSymbol.TypeParameters.Length}];");
            for (int i = 0; i < typeSymbol.TypeParameters.Length; i++)
            {
                var typeParam = typeSymbol.TypeParameters[i];
                writer.AppendLine($"typeArray[{i}] = parameterBasedTypes.TryGetValue(\"{typeParam.Name}\", out var t{i}) ? t{i} : typeof(object);");
            }
            EmitGenericConstraintValidation(writer, typeSymbol.TypeParameters,
                typeSymbol.TypeParameters.Select(tp => $"typeArray[{typeSymbol.TypeParameters.IndexOf(tp)}]").ToArray(), "class");
        }
        
        // Create test data combination
        writer.AppendLine("// Create test data combination with resolved types");
        writer.AppendLine("genericCombinations.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"DisplayName = \"Test with parameter-inferred types\",");
        writer.AppendLine("ResolvedGenericTypes = parameterBasedTypes,");
        writer.AppendLine("ClassDataSourceIndex = 0,");
        writer.AppendLine("MethodDataSourceIndex = 0,");
        writer.AppendLine("ClassLoopIndex = 0,");
        writer.AppendLine("MethodLoopIndex = 0,");
        writer.AppendLine("PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),");
        
        // Generate method data factories for Matrix-based generic tests
        writer.AppendLine("// For Matrix-based tests, delegate to MatrixDataSource at runtime");
        writer.AppendLine("MethodDataFactories = new Func<Task<object?>>[]");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// This will be replaced by runtime Matrix data source generation");
        writer.AppendLine("async () => await global::TUnit.Core.TestDataSourceGenerator.GenerateMatrixDataSourceValueAsync(");
        writer.AppendLine("    testSessionId, parameterBasedTypes)");
        writer.Unindent();
        writer.AppendLine("},");
        
        writer.Unindent();
        writer.AppendLine("});");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("else");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// No typed parameter attributes found");
        writer.AppendLine("genericCombinations.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = new InvalidOperationException(");
        writer.AppendLine("    \"Untyped data source used with generic test but no typed parameter attributes found for type inference\"),");
        writer.AppendLine("DisplayName = \"[GENERIC TYPE INFERENCE FAILED: No typed parameter attributes]\"");
        writer.Unindent();
        writer.AppendLine("});");
        writer.Unindent();
        writer.AppendLine("}");
        
        // Apply repeat count and yield the combinations
        writer.AppendLine();
        writer.AppendLine("// Apply repeat count to parameter-based generic combinations");
        writer.AppendLine("for (var repeatIndex = 0; repeatIndex <= 0; repeatIndex++)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("foreach (var combination in genericCombinations)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("yield return new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("ClassDataFactories = combination.ClassDataFactories,");
        writer.AppendLine("MethodDataFactories = combination.MethodDataFactories,");
        writer.AppendLine("ClassDataSourceIndex = combination.ClassDataSourceIndex,");
        writer.AppendLine("MethodDataSourceIndex = combination.MethodDataSourceIndex,");
        writer.AppendLine("ClassLoopIndex = combination.ClassLoopIndex,");
        writer.AppendLine("MethodLoopIndex = combination.MethodLoopIndex,");
        writer.AppendLine("PropertyValueFactories = combination.PropertyValueFactories,");
        writer.AppendLine("DataGenerationException = combination.DataGenerationException,");
        writer.AppendLine("DisplayName = combination.DisplayName,");
        writer.AppendLine("RepeatIndex = repeatIndex,");
        writer.AppendLine("ResolvedGenericTypes = combination.ResolvedGenericTypes");
        writer.Unindent();
        writer.AppendLine("};");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }
    
    private static void EmitGenericConstraintValidation(
        CodeWriter writer,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        IEnumerable<string> typeArgExpressions,
        string constraintContext)
    {
        writer.AppendLine($"// Validate generic constraints for {constraintContext}");
        writer.AppendLine("var constraintErrors = new List<string>();");
        
        var typeArgExprs = typeArgExpressions.ToArray();
        for (int i = 0; i < Math.Min(typeParameters.Length, typeArgExprs.Length); i++)
        {
            var typeParam = typeParameters[i];
            var typeExpr = typeArgExprs[i];
            
            writer.AppendLine($"// Check constraints for {typeParam.Name}");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"var type = {typeExpr};");
            
            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                writer.AppendLine($"if (type.IsValueType)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a class constraint but '{{type.Name}}' is a value type\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                writer.AppendLine($"if (!type.IsValueType)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a struct constraint but '{{type.Name}}' is not a value type\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check new() constraint
            if (typeParam.HasConstructorConstraint)
            {
                writer.AppendLine($"if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' has a new() constraint but '{{type.Name}}' does not have a parameterless constructor\");");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            // Check base type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                if (constraintType.TypeKind == TypeKind.Interface)
                {
                    var interfaceName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"if (!typeof({interfaceName}).IsAssignableFrom(type))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' must implement '{interfaceName}' but '{{type.Name}}' does not\");");
                    writer.Unindent();
                    writer.AppendLine("}");
                }
                else if (constraintType.TypeKind == TypeKind.Class)
                {
                    var baseClassName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"if (!typeof({baseClassName}).IsAssignableFrom(type))");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"constraintErrors.Add($\"Type parameter '{typeParam.Name}' must derive from '{baseClassName}' but '{{type.Name}}' does not\");");
                    writer.Unindent();
                    writer.AppendLine("}");
                }
            }
            
            writer.Unindent();
            writer.AppendLine("}");
        }
        
        writer.AppendLine("if (constraintErrors.Any())");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("throw new InvalidOperationException($\"Generic constraint validation failed:\\n{string.Join(\"\\n\", constraintErrors)}\");");
        writer.Unindent();
        writer.AppendLine("}");
    }
}
