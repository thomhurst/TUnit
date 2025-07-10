using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

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

        // Get repeat count from RepeatAttribute
        var repeatCount = GetRepeatCount(methodSymbol, typeSymbol);

        if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
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
        for (int i = 0; i < methodDataSources.Length; i++)
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
        for (int i = 0; i < classDataSources.Length; i++)
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

        foreach (var propData in propertyDataSources)
        {
            EmitPropertyDataSource(writer, propData, typeSymbol);
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
                EmitArgumentsAttribute(writer, attr, listName, isClassLevel);
            }
            else if (fullyQualifiedName == "global::TUnit.Core.MethodDataSourceAttribute")
            {
                EmitMethodDataSource(writer, attr, listName, isClassLevel, typeSymbol);
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

    private static void EmitArgumentsAttribute(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel)
    {
        writer.AppendLine("// ArgumentsAttribute");

        try
        {
            var formattedArgs = new List<string>();

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
                formattedArgs.AddRange(attr.ConstructorArguments[0].Values.Select(FormatConstantValue));
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

    private static void EmitMethodDataSource(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// MethodDataSourceAttribute");

        if (attr.ConstructorArguments.Length < 1)
        {
            EmitEmptyCombination(writer, listName);
            return;
        }

        var methodName = attr.ConstructorArguments[0].Value?.ToString();
        if (string.IsNullOrEmpty(methodName))
        {
            EmitEmptyCombination(writer, listName);
            return;
        }

        var isStatic = attr.ConstructorArguments.Length > 1 &&
            attr.ConstructorArguments[1].Value is bool and true;

        writer.AppendLine($"// Calling method: {methodName} (static: {isStatic})");

        if (isStatic)
        {
            EmitStaticMethodDataSource(writer, methodName, listName, isClassLevel, typeSymbol);
        }
        else
        {
            EmitInstanceMethodDataSource(writer, methodName, listName, isClassLevel, typeSymbol);
        }
    }

    private static void EmitStaticMethodDataSource(CodeWriter writer, string methodName, string listName, bool isClassLevel, INamedTypeSymbol typeSymbol)
    {
        var fullyQualifiedTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        writer.AppendLine($"var dataEnumerable = {fullyQualifiedTypeName}.{methodName}();");
        writer.AppendLine("int classLoopCounter = 0;");
        writer.AppendLine("int methodLoopCounter = 0;");
        writer.AppendLine("await foreach (var data in dataEnumerable)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        if (isClassLevel)
        {
            writer.AppendLine("ClassDataFactories = data.Select<object?, Func<object?>>(item => () => item).ToArray(),");
        }
        else
        {
            writer.AppendLine("MethodDataFactories = data.Select<object?, Func<object?>>(item => () => item).ToArray(),");
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

    private static void EmitInstanceMethodDataSource(CodeWriter writer, string methodName, string listName, bool isClassLevel, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine($"// Instance method: {methodName}");
        writer.AppendLine("// Instance methods are not supported in the unified compile-time data generation approach");
        writer.AppendLine("// because they require a test class instance which doesn't exist at compile time.");
        writer.AppendLine("// Consider using static methods for data sources instead.");

        writer.AppendLine("errorCombination = new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("DataGenerationException = new NotSupportedException(");
        writer.AppendLine($"    \"Instance method '{methodName}' cannot be used as a data source. \" +");
        writer.AppendLine("    \"Instance methods require a test class instance which is not available at compile time. \" +");
        writer.AppendLine("    \"Please use static methods for data sources.\"),");
        writer.AppendLine($"DisplayName = \"[INSTANCE METHOD NOT SUPPORTED: {methodName}]\"");
        writer.Unindent();
        writer.AppendLine("};");
    }

    private static void EmitPropertyDataSource(CodeWriter writer, PropertyWithDataSource propData, INamedTypeSymbol typeSymbol)
    {
        try
        {
            var propertyName = propData.Property.Name;
            var attr = propData.DataSourceAttribute;

            if (attr.AttributeClass != null && attr.AttributeClass.GloballyQualifiedNonGeneric() == "global::TUnit.Core.ArgumentsAttribute")
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
            }
        }
        catch
        {
            // Ignore errors in property data source processing
        }
    }

    private static void EmitAsyncUntypedDataSourceGeneratorAttribute(CodeWriter writer, AttributeData attr, string listName, bool isClassLevel, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("// AsyncUntypedDataSourceGeneratorAttribute");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();

        // Create an instance of the generator and call GenerateAsync
        var attributeType = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
        writer.AppendLine($"var generator = new {attributeType}();");
        
        writer.AppendLine("// Create TestInformation for the data generator");
        writer.Append("var testInformation = ");
        TestInformationGenerator.GenerateTestInformation(writer, methodSymbol, typeSymbol);
        writer.AppendLine(";");
        writer.AppendLine();
        
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
        writer.AppendLine($"Type = DataGeneratorType.{(isClassLevel ? "ClassParameters" : "TestParameters")},");
        writer.AppendLine("TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),");
        writer.AppendLine("MembersToGenerate = membersToGenerate,");
        writer.AppendLine("TestInformation = testInformation,");
        writer.AppendLine("TestSessionId = Guid.NewGuid().ToString(),");
        writer.AppendLine("TestClassInstance = null,");
        writer.AppendLine("ClassInstanceArguments = null");
        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("int classLoopCounter = 0;");
        writer.AppendLine("int methodLoopCounter = 0;");
        writer.AppendLine("await foreach (var dataSourceFunc in generator.GenerateAsync(dataGeneratorMetadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Get initial data to determine array length");
        writer.AppendLine("var initialData = await dataSourceFunc();");
        writer.AppendLine("var dataLength = initialData?.Length ?? 0;");
        writer.AppendLine();
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        if (isClassLevel)
        {
            writer.AppendLine("ClassDataFactories = Enumerable.Range(0, dataLength).Select(index => new Func<Task<object?>>(async () => (await dataSourceFunc())?[index])).ToArray(),");
        }
        else
        {
            writer.AppendLine("MethodDataFactories = Enumerable.Range(0, dataLength).Select(index => new Func<Task<object?>>(async () => (await dataSourceFunc())?[index])).ToArray(),");
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

        // Create an instance of the generator and call GenerateDataSources
        var attributeType = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
        writer.AppendLine($"var generator = new {attributeType}();");
        
        writer.AppendLine("// Create TestInformation for the data generator");
        writer.Append("var testInformation = ");
        TestInformationGenerator.GenerateTestInformation(writer, methodSymbol, typeSymbol);
        writer.AppendLine(";");
        writer.AppendLine();
        
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
        writer.AppendLine($"Type = DataGeneratorType.{(isClassLevel ? "ClassParameters" : "TestParameters")},");
        writer.AppendLine("TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),");
        writer.AppendLine("MembersToGenerate = membersToGenerate,");
        writer.AppendLine("TestInformation = testInformation,");
        writer.AppendLine("TestSessionId = Guid.NewGuid().ToString(),");
        writer.AppendLine("TestClassInstance = null,");
        writer.AppendLine("ClassInstanceArguments = null");
        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("int classLoopCounter = 0;");
        writer.AppendLine("int methodLoopCounter = 0;");
        writer.AppendLine("await foreach (var dataSourceFunc in ((IAsyncDataSourceGeneratorAttribute)generator).GenerateAsync(dataGeneratorMetadata))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Get initial data to determine array length");
        writer.AppendLine("var initialData = await dataSourceFunc();");
        writer.AppendLine("var dataLength = initialData?.Length ?? 0;");
        writer.AppendLine();
        writer.AppendLine($"{listName}.Add(new TestDataCombination");
        writer.AppendLine("{");
        writer.Indent();

        if (isClassLevel)
        {
            writer.AppendLine("ClassDataFactories = Enumerable.Range(0, dataLength).Select(index => new Func<Task<object?>>(async () => (await dataSourceFunc())?[index])).ToArray(),");
        }
        else
        {
            writer.AppendLine("MethodDataFactories = Enumerable.Range(0, dataLength).Select(index => new Func<Task<object?>>(async () => (await dataSourceFunc())?[index])).ToArray(),");
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

    private static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        var fullyQualifiedName = attributeClass.GloballyQualifiedNonGeneric();

        // Check direct fully qualified name matches
        if (fullyQualifiedName is "global::TUnit.Core.ArgumentsAttribute"
            or "global::TUnit.Core.MethodDataSourceAttribute")
        {
            return true;
        }

        // Check if this inherits from any data source attribute using the extension method
        return attributeClass.IsOrInherits("global::TUnit.Core.ArgumentsAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.MethodDataSourceAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute");
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
            repeatAttr.ConstructorArguments[0].Value is int repeatTimes && 
            repeatTimes > 0)
        {
            return repeatTimes;
        }

        return 0; // Default: no repeat
    }

    private struct PropertyWithDataSource
    {
        public IPropertySymbol Property { get; init; }
        public AttributeData DataSourceAttribute { get; init; }
    }
}
