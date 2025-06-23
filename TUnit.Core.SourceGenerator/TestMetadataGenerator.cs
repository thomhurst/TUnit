using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Source generator that emits TestMetadata for discovered tests.
/// Generates StaticTestDefinition for AOT-compatible tests and DynamicTestMetadata for others.
/// </summary>
[Generator]
public class TestMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all test methods
        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null);

        // Generate a separate file for each test method to avoid Collect()
        context.RegisterSourceOutput(testMethods, GenerateTestRegistration);
    }

    private static TestMethodInfo? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;

        // Skip abstract classes, static methods, and open generic types
        if (typeSymbol.IsAbstract || methodSymbol.IsStatic || typeSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            return null;
        }

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        return new TestMethodInfo
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = typeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes[0]
        };
    }

    private static void GenerateTestRegistration(SourceProductionContext context, TestMethodInfo? testInfo)
    {
        if (testInfo == null)
        {
            return;
        }

        try
        {

        using var writer = new CodeWriter();

        // Generate a unique identifier for this test
        var guid = Guid.NewGuid().ToString("N");
        var className = GetFullTypeName(testInfo.TypeSymbol);
        var methodName = testInfo.MethodSymbol.Name;
        // Sanitize class and method names for use in filenames
        var safeClassName = SanitizeForFilename(className);
        var safeMethodName = SanitizeForFilename(methodName);

        // Check for required properties
        var requiredProperties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsRequired)
            .ToList();

        // Check for constructor parameters
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;

        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Reflection;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine();
        writer.AppendLine("using global::TUnit.Core.SourceGenerator;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        using (writer.BeginBlock($"internal static class TestMetadataRegistry_{safeClassName}_{safeMethodName}_{guid}"))
        {
            writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.BeginBlock("public static void Initialize()"))
            {
                // Determine if we can use StaticTestDefinition (AOT-compatible)
                bool canUseStaticDefinition = DetermineIfStaticTestDefinition(testInfo);

                if (canUseStaticDefinition)
                {
                    writer.AppendLine("var testDescriptors = new System.Collections.Generic.List<ITestDescriptor>();");
                    writer.AppendLine();
                    GenerateStaticTestDefinitions(writer, testInfo, className, methodName, requiredProperties, constructorWithParameters, hasParameterlessConstructor);
                    writer.AppendLine();
                    writer.AppendLine("TestSourceRegistrar.RegisterTests(testDescriptors);");
                }
                else
                {
                    writer.AppendLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
                    writer.AppendLine();
                    GenerateDynamicTestMetadata(writer, testInfo, className, methodName, requiredProperties, constructorWithParameters, hasParameterlessConstructor);
                    writer.AppendLine();
                    writer.AppendLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
                }
            }
        }

        // Add the generated code to the compilation
        context.AddSource($"{safeClassName}_{safeMethodName}_{guid}.g.cs", writer.ToString());
        }
        catch (Exception ex)
        {
            // Report diagnostic instead of throwing
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TSG001",
                    "Source generation failed",
                    "Failed to generate test metadata for {0}.{1}: {2}",
                    "TUnit.SourceGenerator",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                Location.None,
                testInfo.TypeSymbol.Name,
                testInfo.MethodSymbol.Name,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool DetermineIfStaticTestDefinition(TestMethodInfo testInfo)
    {
        // Can use static definition if:
        // 1. The type is not generic
        if (testInfo.TypeSymbol.IsGenericType)
            return false;

        // 2. The method is not generic
        if (testInfo.MethodSymbol.IsGenericMethod)
            return false;

        // 3. No data sources that require runtime resolution
        // Check method parameters for data attributes
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            foreach (var attr in param.GetAttributes())
            {
                // Check if it's a data source attribute that requires runtime resolution
                if (IsRuntimeDataSourceAttribute(attr))
                    return false;
            }
        }

        // Check class constructor parameters for data attributes
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public);

        foreach (var constructor in constructors)
        {
            foreach (var param in constructor.Parameters)
            {
                foreach (var attr in param.GetAttributes())
                {
                    if (IsRuntimeDataSourceAttribute(attr))
                        return false;
                }
            }
        }

        // Check properties for data attributes
        var properties = testInfo.TypeSymbol.GetMembers().OfType<IPropertySymbol>();
        foreach (var prop in properties)
        {
            foreach (var attr in prop.GetAttributes())
            {
                if (IsRuntimeDataSourceAttribute(attr))
                    return false;
            }
        }

        // All checks passed - can use static definition
        return true;
    }

    private static bool IsRuntimeDataSourceAttribute(AttributeData attr)
    {
        var attrName = attr.AttributeClass?.Name;

        // These require runtime resolution:
        // - GeneratedDataAttribute (dynamic generation)
        // - Attributes inheriting from AsyncDataSourceGeneratorAttribute (async generation)
        // - Attributes inheriting from DataSourceGeneratorAttribute (dynamic generation)
        if (attrName is "GeneratedDataAttribute")
            return true;
            
        // Check if it inherits from async/sync data source generator attributes
        var baseType = attr.AttributeClass?.BaseType;
        while (baseType != null)
        {
            var baseName = baseType.Name;
            if (baseName is "AsyncDataSourceGeneratorAttribute" or "DataSourceGeneratorAttribute" 
                or "AsyncNonTypedDataSourceGeneratorAttribute" or "NonTypedDataSourceGeneratorAttribute")
                return true;
            baseType = baseType.BaseType;
        }
        
        return false;
    }
    
    private static bool IsCompileTimeDataSourceAttribute(AttributeData attr)
    {
        var attrName = attr.AttributeClass?.Name;
        
        // These can be handled at compile time through code generation:
        // - ArgumentsAttribute (direct data)
        // - MethodDataSourceAttribute (generate lambda to call method)
        // - Attributes inheriting from AsyncDataSourceGeneratorAttribute (generate lambda to instantiate and call)
        
        if (attrName is "ArgumentsAttribute" or "MethodDataSourceAttribute")
            return true;
            
        // Check if it inherits from AsyncDataSourceGeneratorAttribute
        var baseType = attr.AttributeClass?.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AsyncDataSourceGeneratorAttribute")
                return true;
            baseType = baseType.BaseType;
        }
        
        return false;
    }

    private static void GenerateStaticTestDefinitions(
        CodeWriter writer,
        TestMethodInfo testInfo,
        string className,
        string methodName,
        List<IPropertySymbol> requiredProperties,
        IMethodSymbol? constructorWithParameters,
        bool hasParameterlessConstructor)
    {
        // Get all data source attributes that can be handled at compile time
        var classDataAttrs = testInfo.TypeSymbol.GetAttributes()
            .Where(attr => IsCompileTimeDataSourceAttribute(attr))
            .ToList();
            
        var methodDataAttrs = testInfo.MethodSymbol.GetAttributes()
            .Where(attr => IsCompileTimeDataSourceAttribute(attr))
            .ToList();

        // If no attributes, create one test with empty data providers
        if (!classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            GenerateSingleStaticTestDefinition(writer, testInfo, className, methodName, 
                requiredProperties, constructorWithParameters, hasParameterlessConstructor,
                null, null, 0);
            return;
        }

        // Generate test definitions for each combination
        var testIndex = 0;
        
        // If we have class data but no method data
        if (classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            foreach (var classAttr in classDataAttrs)
            {
                GenerateSingleStaticTestDefinition(writer, testInfo, className, methodName,
                    requiredProperties, constructorWithParameters, hasParameterlessConstructor,
                    classAttr, null, testIndex++);
            }
        }
        // If we have method data but no class data
        else if (!classDataAttrs.Any() && methodDataAttrs.Any())
        {
            foreach (var methodAttr in methodDataAttrs)
            {
                GenerateSingleStaticTestDefinition(writer, testInfo, className, methodName,
                    requiredProperties, constructorWithParameters, hasParameterlessConstructor,
                    null, methodAttr, testIndex++);
            }
        }
        // If we have both class and method data - create cartesian product
        else
        {
            foreach (var classAttr in classDataAttrs)
            {
                foreach (var methodAttr in methodDataAttrs)
                {
                    GenerateSingleStaticTestDefinition(writer, testInfo, className, methodName,
                        requiredProperties, constructorWithParameters, hasParameterlessConstructor,
                        classAttr, methodAttr, testIndex++);
                }
            }
        }
    }

    private static void GenerateSingleStaticTestDefinition(
        CodeWriter writer,
        TestMethodInfo testInfo,
        string className,
        string methodName,
        List<IPropertySymbol> requiredProperties,
        IMethodSymbol? constructorWithParameters,
        bool hasParameterlessConstructor,
        AttributeData? classArguments,
        AttributeData? methodArguments,
        int testIndex)
    {
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);

        using (writer.BeginObjectInitializer("var staticDef = new StaticTestDefinition"))
        {
            writer.AppendLine($"TestId = \"{className}.{methodName}_{testIndex}_{{{{TestIndex}}}}\",");
            writer.AppendLine($"DisplayName = \"{methodName}\",");
            writer.AppendLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {testInfo.LineNumber},");
            writer.AppendLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
            writer.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
            writer.AppendLine($"SkipReason = {skipReason},");
            writer.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)},");
            writer.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
            writer.AppendLine($"TestClassType = typeof({className}),");
            writer.AppendLine($"TestMethodInfo = typeof({className}).GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!,");

            // Generate class factory with CastHelper
            writer.AppendLine($"ClassFactory = {GenerateStaticClassFactory(testInfo.TypeSymbol, className, constructorWithParameters, hasParameterlessConstructor)},");

            // Generate method invoker with CastHelper
            writer.AppendLine($"MethodInvoker = {GenerateStaticMethodInvoker(testInfo.MethodSymbol, className)},");

            // Generate property values provider
            writer.AppendLine($"PropertyValuesProvider = {GenerateStaticPropertyValuesProvider(testInfo.TypeSymbol)},");

            // Generate data providers
            writer.AppendLine($"ClassDataProvider = {GenerateClassDataProviderForAttribute(classArguments, className)},");
            writer.AppendLine($"MethodDataProvider = {GenerateMethodDataProviderForAttribute(methodArguments, className)}");
        }

        writer.AppendLine();
        writer.AppendLine("testDescriptors.Add(staticDef);");
    }

    private static void GenerateDynamicTestMetadata(
        CodeWriter writer,
        TestMethodInfo testInfo,
        string className,
        string methodName,
        List<IPropertySymbol> requiredProperties,
        IMethodSymbol? constructorWithParameters,
        bool hasParameterlessConstructor)
    {
        // Extract skip information
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);

        // For generic types, we can't use typeof() so TestClassType will be null
        var testClassTypeValue = testInfo.TypeSymbol.IsGenericType ? "null" : $"typeof({className})";

        // Create the test metadata object first without problematic array properties
        using (writer.BeginObjectInitializer("var metadata = new DynamicTestMetadata"))
        {
            writer.AppendLine($"TestIdTemplate = \"{className}.{methodName}_{{{{TestIndex}}}}\",");
            writer.AppendLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
            writer.AppendLine($"TestClassType = {testClassTypeValue},");

            using (writer.BeginObjectInitializer("MethodMetadata = new MethodMetadata", ","))
            {
                writer.AppendLine($"Name = \"{testInfo.MethodSymbol.Name}\",");
                writer.AppendLine($"Type = {testClassTypeValue} ?? typeof(object),");
                writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
                writer.AppendLine($"Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(testInfo.MethodSymbol)},");
                writer.AppendLine($"GenericTypeCount = {testInfo.MethodSymbol.TypeParameters.Length},");

                using (writer.BeginObjectInitializer("Class = new ClassMetadata", ","))
                {
                    writer.AppendLine($"Name = \"{testInfo.TypeSymbol.Name}\",");
                    writer.AppendLine($"Type = {testClassTypeValue} ?? typeof(object),");
                    writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
                    writer.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.GetAttributes(), testInfo.TypeSymbol, writer._indentLevel)},");
                    writer.AppendLine($"Namespace = \"{testInfo.TypeSymbol.ContainingNamespace}\",");
                    writer.AppendLine($"Assembly = new AssemblyMetadata {{ Name = \"{testInfo.TypeSymbol.ContainingAssembly.Name}\", Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.ContainingAssembly.GetAttributes(), testInfo.TypeSymbol.ContainingAssembly, writer._indentLevel)} }},");
                    writer.AppendLine("Parameters = System.Array.Empty<ParameterMetadata>(),");
                    writer.AppendLine($"Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(testInfo.TypeSymbol)},");
                    writer.AppendLine("Parent = null");
                }
                writer.AppendLine($"ReturnType = {(ContainsTypeParameter(testInfo.MethodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(testInfo.MethodSymbol)})")},");
                writer.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.MethodSymbol.ReturnType)},");
                writer.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.MethodSymbol.GetAttributes(), testInfo.MethodSymbol, writer._indentLevel)}");
            }
            writer.AppendLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {testInfo.LineNumber},");
            writer.AppendLine($"TestClassFactory = {GenerateTestClassFactory(testInfo.TypeSymbol, className, requiredProperties, constructorWithParameters, hasParameterlessConstructor)},");
            writer.AppendLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(testInfo.TypeSymbol)},");
            writer.AppendLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(testInfo.MethodSymbol)},");
            writer.AppendLine($"PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(testInfo.TypeSymbol)},");
            writer.AppendLine($"DisplayNameTemplate = \"{methodName}\",");
            writer.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
            writer.AppendLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
            writer.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
            writer.AppendLine($"SkipReason = {skipReason},");
            writer.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)}");
        }
        writer.AppendLine();
        writer.AppendLine("testMetadata.Add(metadata);");
    }

    private static string GenerateStaticClassFactory(
        INamedTypeSymbol typeSymbol,
        string className,
        IMethodSymbol? constructorWithParameters,
        bool hasParameterlessConstructor)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("args => ");

        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            writer.Append($"new {className}(");
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"TUnit.Core.Helpers.CastHelper.Cast<{typeName}>(args[{i}])";
                }));
            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            writer.Append($"new {className}()");
        }

        return writer.ToString().Trim();
    }

    private static string GenerateStaticMethodInvoker(IMethodSymbol methodSymbol, string className)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        var methodName = methodSymbol.Name;

        writer.Append("async (instance, args) => ");
        writer.Append("{");

        // Cast instance to the correct type
        writer.Append($" var typedInstance = ({className})instance;");

        // Use AsyncConvert.Convert to handle the method invocation
        writer.Append($" await TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}(");

        // Add parameters with CastHelper
        if (methodSymbol.Parameters.Length > 0)
        {
            var paramList = string.Join(", ", methodSymbol.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"TUnit.Core.Helpers.CastHelper.Cast<{typeName}>(args[{i}])";
                }));
            writer.Append(paramList);
        }

        writer.Append("));");
        writer.Append(" }");

        return writer.ToString().Trim();
    }

    private static string GenerateStaticPropertyValuesProvider(INamedTypeSymbol typeSymbol)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        // Find properties with MethodDataSource attribute (used for property data sources)
        var propertiesWithDataSource = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute"))
            .ToList();

        if (!propertiesWithDataSource.Any())
        {
            // No properties with data sources - return empty dictionary
            return "() => new[] { new System.Collections.Generic.Dictionary<string, object?>() }";
        }

        // Generate a provider that combines all property data sources
        writer.Append("() => ");
        writer.Append("{");

        // Generate calls to get data for each property
        foreach (var prop in propertiesWithDataSource)
        {
            var dataSourceAttr = prop.GetAttributes()
                .First(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

            if (dataSourceAttr.ConstructorArguments.Length > 0)
            {
                var sourceName = dataSourceAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(sourceName))
                {
                    // Find the data source (method or property)
                    var member = typeSymbol.GetMembers(sourceName!).FirstOrDefault();

                    if (member is IMethodSymbol method)
                    {
                        writer.Append($" var {prop.Name}Data = ");
                        GenerateDataSourceCall(writer, method, typeSymbol);
                        writer.Append(";");
                    }
                    else if (member is IPropertySymbol sourceProp)
                    {
                        writer.Append($" var {prop.Name}Data = ");
                        GeneratePropertyDataSourceCall(writer, sourceProp, typeSymbol);
                        writer.Append(";");
                    }
                }
            }
        }

        // Combine all data sources into dictionaries
        writer.Append(" return ");

        if (propertiesWithDataSource.Count == 1)
        {
            var prop = propertiesWithDataSource[0];
            writer.Append($"{prop.Name}Data.Select(value => new System.Collections.Generic.Dictionary<string, object?>() {{ {{ \"{prop.Name}\", value }} }})");
        }
        else
        {
            // TODO: Handle multiple property data sources (cartesian product)
            writer.Append("new[] { new System.Collections.Generic.Dictionary<string, object?>() }");
        }

        writer.Append(";");
        writer.Append(" }");

        return writer.ToString().Trim();
    }

    private static void GenerateDataSourceCall(CodeWriter writer, IMethodSymbol method, INamedTypeSymbol containingType)
    {
        if (method.IsStatic)
        {
            writer.Append($"{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{method.Name}()");
        }
        else
        {
            writer.Append($"new {containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}().{method.Name}()");
        }
    }

    private static void GeneratePropertyDataSourceCall(CodeWriter writer, IPropertySymbol property, INamedTypeSymbol containingType)
    {
        if (property.IsStatic)
        {
            writer.Append($"{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{property.Name}");
        }
        else
        {
            writer.Append($"new {containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}().{property.Name}");
        }
    }

    // Existing helper methods from original generator
    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string SanitizeForFilename(string name)
    {
        // Replace all invalid filename characters with underscores
        var invalid = System.IO.Path.GetInvalidFileNameChars()
            .Concat(['<', '>', '(', ')', '[', ']', '{', '}', ',', ' ', '`', '.'])
            .Distinct();

        var sanitized = name;
        foreach (var c in invalid)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }

    private static string GetReturnTypeName(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return "void";
        }

        return returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    private static string GenerateTestClassFactory(INamedTypeSymbol typeSymbol, string className, List<IPropertySymbol> requiredProperties, IMethodSymbol? constructorWithParameters, bool hasParameterlessConstructor)
    {
        // For generic types, we can't create instances at compile time
        if (typeSymbol.IsGenericType)
        {
            return "null!"; // Will be replaced at runtime by TestBuilder
        }

        // If there are required properties with data sources, we need special handling
        var requiredPropertiesWithDataSource = requiredProperties
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true))
            .ToList();

        if (requiredPropertiesWithDataSource.Any())
        {
            return GenerateFactoryWithRequiredProperties(typeSymbol, className, requiredProperties, constructorWithParameters, hasParameterlessConstructor);
        }

        // Simple factory without required properties
        using var writer = new CodeWriter("", includeHeader: false);
        // No need to indent here - this generates inline code
        writer.Append("args => ");

        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Append($"new {className}(");

            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));

            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            // Simple parameterless constructor
            writer.Append($"new {className}()");
        }

        return writer.ToString().Trim(); // Trim to remove any extra newlines
    }

    private static string GenerateFactoryWithRequiredProperties(INamedTypeSymbol typeSymbol, string className, List<IPropertySymbol> requiredProperties, IMethodSymbol? constructorWithParameters, bool hasParameterlessConstructor)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        // No need to indent here - this generates inline code
        writer.Append("args => ");

        // Create a new instance with all required properties initialized
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            writer.Append($"new {className}(");
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));
            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            writer.Append($"new {className}()");
        }

        // Always add object initializer for required properties
        writer.Append(" { ");
        var propertyInitializers = requiredProperties.Select(prop =>
        {
            // For properties with data sources, create a minimal valid instance
            // that satisfies the compiler but will be replaced at runtime
            var defaultValue = GetDataSourceAwareDefaultValue(prop);
            return $"{prop.Name} = {defaultValue}";
        });
        writer.Append(string.Join(", ", propertyInitializers));
        writer.Append(" }");

        return writer.ToString().Trim();
    }

    private static string GetDataSourceAwareDefaultValue(IPropertySymbol property)
    {
        var type = property.Type;

        // For reference types, try to create a new instance
        if (type.IsReferenceType)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Special handling for common types
            if (type.SpecialType == SpecialType.System_String)
            {
                return "string.Empty";
            }

            // Check if the type has a parameterless constructor
            if (type is INamedTypeSymbol namedType)
            {
                var hasParameterlessConstructor = namedType.Constructors
                    .Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);

                if (hasParameterlessConstructor)
                {
                    return $"new {typeName}()";
                }
            }

            // Fallback to null with suppression
            return "null!";
        }

        // Use the existing logic for value types
        return GetDefaultValueForType(type);
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

    private static string GetDefaultValueForType(ITypeSymbol type)
    {
        return $"default({type.GloballyQualified()})";
    }

    private static string GenerateClassDataProviderForAttribute(AttributeData? attribute, string className)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        if (attribute == null)
        {
            writer.Append("new TUnit.Core.EmptyDataProvider()");
            return writer.ToString().Trim();
        }

        var attrName = attribute.AttributeClass?.Name;
        
        if (attrName == "ArgumentsAttribute")
        {
            writer.Append("new TUnit.Core.ArgumentsDataProvider(");
            GenerateArgumentsForAttribute(writer, attribute);
            writer.Append(")");
        }
        else if (attrName == "MethodDataSourceAttribute")
        {
            GenerateMethodDataSourceProvider(writer, attribute, className);
        }
        else if (IsAsyncDataSourceGenerator(attribute))
        {
            GenerateAsyncDataSourceProvider(writer, attribute);
        }
        else
        {
            writer.Append("new TUnit.Core.EmptyDataProvider()");
        }

        return writer.ToString().Trim();
    }

    private static string GenerateMethodDataProviderForAttribute(AttributeData? attribute, string className)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        if (attribute == null)
        {
            writer.Append("new TUnit.Core.EmptyDataProvider()");
            return writer.ToString().Trim();
        }

        var attrName = attribute.AttributeClass?.Name;
        
        if (attrName == "ArgumentsAttribute")
        {
            writer.Append("new TUnit.Core.ArgumentsDataProvider(");
            GenerateArgumentsForAttribute(writer, attribute);
            writer.Append(")");
        }
        else if (attrName == "MethodDataSourceAttribute")
        {
            GenerateMethodDataSourceProvider(writer, attribute, className);
        }
        else if (IsAsyncDataSourceGenerator(attribute))
        {
            GenerateAsyncDataSourceProvider(writer, attribute);
        }
        else
        {
            writer.Append("new TUnit.Core.EmptyDataProvider()");
        }

        return writer.ToString().Trim();
    }

    private static void GenerateArgumentsForAttribute(CodeWriter writer, AttributeData attribute)
    {
        var args = attribute.ConstructorArguments;
        // ArgumentsAttribute constructor takes params object?[]
        if (args.Length == 1 && args[0].Kind == TypedConstantKind.Array)
        {
            // Handle params array case
            var values = args[0].Values;
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) writer.Append(", ");
                writer.Append(TypedConstantParser.GetRawTypedConstantValue(values[i]));
            }
        }
        else
        {
            // Handle individual arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) writer.Append(", ");
                writer.Append(TypedConstantParser.GetRawTypedConstantValue(args[i]));
            }
        }
    }
    
    private static bool IsAsyncDataSourceGenerator(AttributeData attribute)
    {
        var baseType = attribute.AttributeClass?.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AsyncDataSourceGeneratorAttribute")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }
    
    private static void GenerateMethodDataSourceProvider(CodeWriter writer, AttributeData attribute, string className)
    {
        // MethodDataSourceAttribute constructor: (Type? type, string methodName) or (string methodName)
        var args = attribute.ConstructorArguments;
        
        writer.Append("new TUnit.Core.MethodDataProvider(() => ");
        
        if (args.Length == 2 && args[0].Value != null)
        {
            // Type and method name
            var type = args[0].Value as ITypeSymbol;
            var methodName = args[1].Value?.ToString();
            writer.Append($"{type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{methodName}()");
        }
        else if (args.Length == 1 || (args.Length == 2 && args[0].Value == null))
        {
            // Just method name - refers to a static method on the test class
            var methodName = args.Length == 1 ? args[0].Value?.ToString() : args[1].Value?.ToString();
            writer.Append($"{className}.{methodName}()");
        }
        
        writer.Append(")");
    }
    
    private static void GenerateAsyncDataSourceProvider(CodeWriter writer, AttributeData attribute)
    {
        // For async generators, we need to instantiate the attribute and call GenerateAsync
        var attrType = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        
        writer.Append("new TUnit.Core.AsyncDataGeneratorProvider(new ");
        writer.Append(attrType ?? "object");
        writer.Append("()");
        writer.Append(")");
    }

    private class TestMethodInfo
    {
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public INamedTypeSymbol TypeSymbol { get; set; } = null!;
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public AttributeData TestAttribute { get; set; } = null!;
    }
}
