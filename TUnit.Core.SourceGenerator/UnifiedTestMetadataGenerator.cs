using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Configuration;
using TUnit.Core.SourceGenerator.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Simplified source generator that emits unified TestMetadata with AOT support
/// </summary>
// [Generator] // Disabled - replaced by UnifiedTestMetadataGeneratorV2
public class UnifiedTestMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get configuration from analyzer options
        var configurationProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => TUnitConfiguration.Create(options.GlobalOptions));

        // Find all test methods
        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null);

        // Combine configuration with test methods
        var combined = testMethods.Collect().Combine(configurationProvider);

        // Generate the test registry with configuration
        context.RegisterSourceOutput(combined, GenerateTestRegistryWithConfiguration);
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;
        if (typeSymbol == null || typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return null;
        }

        // Skip abstract classes and static methods
        if (namedTypeSymbol.IsAbstract || methodSymbol.IsStatic)
        {
            return null;
        }

        // Skip generic type definitions with unresolved type parameters
        // These will be handled through InheritsTests or explicit instantiation
        if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Any(t => t is ITypeParameterSymbol))
        {
            return null;
        }

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Skip tests on base classes if derived class has InheritsTests attribute
        // This prevents duplicate test registration
        if (IsTestInheritedByDerivedClass(methodSymbol, namedTypeSymbol))
        {
            return null;
        }

        // Skip tests with AsyncDataSourceGenerator attributes - they're handled by specialized generators
        var hasAsyncDataSourceGenerator = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass != null && IsAsyncDataSourceGeneratorAttribute(a.AttributeClass));

        if (hasAsyncDataSourceGenerator)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        // Make sure we have at least one attribute (the Test attribute)
        if (context.Attributes.Length == 0)
        {
            return null;
        }

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = namedTypeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes[0],
            Context = context
        };
    }

    private static bool IsAsyncDataSourceGeneratorAttribute(INamedTypeSymbol attributeClass)
    {
        // Check if the attribute inherits from AsyncDataSourceGeneratorAttribute
        var baseType = attributeClass.BaseType;
        while (baseType != null)
        {
            if (baseType.Name.StartsWith("AsyncDataSourceGeneratorAttribute") &&
                baseType.ContainingNamespace?.ToString() == "TUnit.Core")
            {
                return true;
            }

            // Also check interfaces
            if (baseType.Interfaces.Any(i => i.Name == "IAsyncDataSourceGeneratorAttribute" &&
                i.ContainingNamespace?.ToString() == "TUnit.Core"))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static void GenerateTestRegistryWithConfiguration(SourceProductionContext context, (ImmutableArray<TestMethodMetadata?> TestMethods, TUnitConfiguration Configuration) input)
    {
        try
        {
            var (testMethods, configuration) = input;
            var validTests = testMethods
                .Where(t => t != null)
                .Cast<TestMethodMetadata>()
                .Where(t => !t.TypeSymbol.IsAbstract) // Filter out abstract classes
                .ToList();
            if (!validTests.Any())
            {
                return;
            }
            
            var diagnosticContext = new DiagnosticContext(context);
            
            // Initialize generic type resolver with unlimited depth (no longer configurable)
            var genericResolver = new GenericTypeResolver(int.MaxValue);
            
            // Process generic types
            var testsByClass = validTests.GroupBy(t => t.TypeSymbol, SymbolEqualityComparer.Default);
            foreach (var classGroup in testsByClass)
            {
                if (classGroup.Key is INamedTypeSymbol namedType && namedType.IsGenericType)
                {
                    genericResolver.ResolveGenericTypes(namedType, classGroup.ToList());
                }
            }
            
            using var writer = new CodeWriter();
            
            // Report configuration if verbose diagnostics enabled
            if (configuration.EnableVerboseDiagnostics)
            {
                diagnosticContext.ReportInfo(
                    "TUNIT_CONFIG_001",
                    "TUnit Configuration",
                    $"Source generation running with optimal defaults: AOT-only=true, PropertyInjection=true, ValueTask=true, UnlimitedGenericDepth=true");
            }

            // Write file header
            writer.AppendLine("#nullable enable");
            writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
            writer.AppendLine();
            writer.AppendLine("using System;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine("using System.Linq;");
            writer.AppendLine("using System.Runtime.CompilerServices;");
            writer.AppendLine("using System.Threading;");
            writer.AppendLine("using System.Threading.Tasks;");
            writer.AppendLine("using global::TUnit.Core;");
            writer.AppendLine("using global::TUnit.Engine;");
            writer.AppendLine();

            writer.AppendLine("namespace TUnit.Generated;");
            writer.AppendLine();

            // Generate the registry class
            using (writer.BeginBlock("public static class UnifiedTestMetadataRegistry"))
            {
                // Generate static test metadata collection
                writer.AppendLine("private static readonly List<TestMetadata> _allTests = new();");
                writer.AppendLine();
                writer.AppendLine("/// <summary>");
                writer.AppendLine("/// Gets all registered test metadata");
                writer.AppendLine("/// </summary>");
                writer.AppendLine("public static IReadOnlyList<TestMetadata> AllTests => _allTests;");
                writer.AppendLine();

                // Module initializer to register all tests
                writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
                using (writer.BeginBlock("public static void Initialize()"))
                {
                    writer.AppendLine("try");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine("RegisterAllDelegates();");
                    writer.AppendLine("RegisterDataSourceFactories();");
                    writer.AppendLine("RegisterAllTests();");
                    writer.AppendLine();
                    writer.AppendLine("// Register with DirectTestMetadataProvider");
                    writer.AppendLine("global::TUnit.Core.DirectTestMetadataProvider.RegisterMetadataProvider(() => AllTests);");
                    writer.Unindent();
                    writer.AppendLine("}");
                    writer.AppendLine("catch (Exception ex)");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine("Console.Error.WriteLine($\"Failed to initialize test registry: {ex}\");");
                    writer.AppendLine("throw;");
                    writer.Unindent();
                    writer.AppendLine("}");
                }

                writer.AppendLine();

                // Generate delegate registration method
                using (writer.BeginBlock("private static void RegisterAllDelegates()"))
                {
                    writer.AppendLine($"// Registering delegates for AOT execution");
                    
                    // Track registered data sources to avoid duplicates
                    var registeredDataSources = new HashSet<string>();
                    
                    foreach (var testInfo in validTests)
                    {
                        var genContext = TestMetadataGenerationContext.Create(testInfo);
                        if (genContext.CanUseStaticDefinition && !testInfo.MethodSymbol.IsGenericMethod && !testInfo.TypeSymbol.IsGenericType)
                        {
                            // Register instance factory
                            if (!genContext.HasParameterlessConstructor && genContext.ConstructorWithParameters != null)
                            {
                                writer.AppendLine($"TestDelegateStorage.RegisterInstanceFactory(\"{genContext.ClassName}\", {genContext.SafeClassName}_InstanceFactory);");
                            }
                            else
                            {
                                writer.AppendLine($"TestDelegateStorage.RegisterInstanceFactory(\"{genContext.ClassName}\", args => new {genContext.ClassName}());");
                            }
                            
                            // Register test invoker
                            writer.AppendLine($"TestDelegateStorage.RegisterTestInvoker(\"{genContext.ClassName}.{genContext.MethodName}\", {genContext.SafeClassName}_{genContext.SafeMethodName}_Invoker);");
                        }
                        
                        // Register data source factories
                        // Note: Data source factories are now registered by GenerateDataSourceFactoriesV2
                        // RegisterDataSourceFactories(writer, testInfo, registeredDataSources);
                    }
                    
                    // Register hook delegates for property injection
                    RegisterPropertyInjectionHooks(writer, validTests, configuration);
                }
                
                writer.AppendLine();

                // Generate the registration method
                using (writer.BeginBlock("private static void RegisterAllTests()"))
                {
                    writer.AppendLine($"// Registering {validTests.Count} tests");
                    writer.AppendLine($"Console.Error.WriteLine(\"Registering {validTests.Count} tests...\");");
                    writer.AppendLine();

                    foreach (var testInfo in validTests)
                    {
                        if (testInfo.MethodSymbol.IsGenericMethod)
                        {
                            GenerateGenericTestMetadata(writer, testInfo);
                        }
                        else
                        {
                            GenerateTestMetadata(writer, testInfo);
                        }
                        writer.AppendLine();
                    }

                    writer.AppendLine($"Console.Error.WriteLine(\"All {validTests.Count} tests registered successfully\");");
                }

                writer.AppendLine();

                // Generate the conversion helper for data sources
                GenerateConversionHelper(writer);

                // Generate strongly-typed delegates inline
                GenerateStronglyTypedDelegatesInline(writer, validTests, diagnosticContext);
                
                // Generate typed property setters inline
                GenerateTypedPropertySettersInline(writer, validTests, diagnosticContext);
                
                // Generate generic type instantiations if any
                var genericInstantiations = genericResolver.GenerateGenericInstantiations();
                if (!string.IsNullOrWhiteSpace(genericInstantiations))
                {
                    writer.AppendLine();
                    writer.AppendLine("// Generic type instantiations");
                    writer.AppendLine("private static readonly Dictionary<Type[], Func<object>> GenericTypeMap = new(TypeArrayComparer.Instance);");
                    writer.AppendLine();
                    writer.AppendLine("static UnifiedTestMetadataRegistry()");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendRaw(genericInstantiations);
                    writer.Unindent();
                    writer.AppendLine("}");
                }

                // Generate helper methods for each test class
                var testClasses = validTests.GroupBy(t => t.TypeSymbol, SymbolEqualityComparer.Default);
                foreach (var classGroup in testClasses)
                {
                    if (classGroup.Key is INamedTypeSymbol namedType)
                    {
                        GenerateTestClassHelpers(writer, namedType, classGroup.ToList(), diagnosticContext, configuration);
                    }
                }
            }

            context.AddSource("UnifiedTestMetadataRegistry.g.cs", writer.ToString());
            
            // Module initializer is now included in the unified registry, no need for separate file
        }
        catch (Exception ex)
        {
            // Generate diagnostic error code that will help identify the issue
            var descriptor = new DiagnosticDescriptor(
                "TUG001",
                "Test metadata generation failed",
                "Failed to generate test metadata: {0}",
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.ToString()));
        }
    }

    private static void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        TestMetadataGenerationContext context;
        try
        {
            context = TestMetadataGenerationContext.Create(testInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create context for {testInfo?.TypeSymbol?.Name}.{testInfo?.MethodSymbol?.Name}: {ex.Message}", ex);
        }

        var paramTypeNames = testInfo.MethodSymbol.Parameters
            .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
            .ToArray();
        var testId = paramTypeNames.Length > 0
            ? $"{context.ClassName}.{context.MethodName}({string.Join(",", paramTypeNames)})"
            : $"{context.ClassName}.{context.MethodName}";

        using (writer.BeginBlock("_allTests.Add(new TestMetadata"))
        {
            writer.AppendLine($"TestId = \"{testId}\",");
            writer.AppendLine($"TestName = \"{context.MethodName}\",");

            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");

            writer.AppendLine($"TestMethodName = \"{context.MethodName}\",");

            try
            {
                // Categories
                GenerateCategories(writer, testInfo);

                // Skip status
                GenerateSkipStatus(writer, testInfo);

                // Timeout
                GenerateTimeout(writer, testInfo);

                // Retry count
                writer.AppendLine($"RetryCount = {GetRetryCount(testInfo)},");

                // Parallelization
                writer.AppendLine($"CanRunInParallel = {GetCanRunInParallel(testInfo).ToString().ToLower()},");

                // Dependencies
                //GenerateDependencies(writer, testInfo);
                writer.AppendLine("DependsOn = Array.Empty<string>(),");

                // Data sources
                GenerateDataSources(writer, testInfo);

                // Class data sources
                GenerateClassDataSources(writer, testInfo);

                // Property data sources
                GeneratePropertyDataSources(writer, testInfo);

                // Parameter info
                writer.AppendLine($"ParameterCount = {testInfo.MethodSymbol.Parameters.Length},");
                GenerateParameterTypes(writer, testInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate metadata fields for {context.ClassName}.{context.MethodName}: {ex.Message}", ex);
            }

            // Check if we should skip instance factory generation (same logic as in GenerateInstanceFactory)
            var hasPropertyDataSources = testInfo.TypeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true));

            var shouldSkipInstanceFactory = hasPropertyDataSources || context.RequiredProperties.Count > 0;

            // AOT factories (if possible)
            if (context.CanUseStaticDefinition &&
                !testInfo.MethodSymbol.IsGenericMethod && !testInfo.TypeSymbol.IsGenericType)
            {
                // Use delegates from centralized storage
                if (shouldSkipInstanceFactory)
                {
                    writer.AppendLine("InstanceFactory = null, // Let TestBuilder handle property injection");
                }
                else
                {
                    writer.AppendLine($"InstanceFactory = TestDelegateStorage.GetInstanceFactory(\"{context.ClassName}\"),");
                }

                writer.AppendLine($"TestInvoker = TestDelegateStorage.GetTestInvoker(\"{context.ClassName}.{context.MethodName}\"),");
            }
            else
            {
                writer.AppendLine("InstanceFactory = null,");
                writer.AppendLine("TestInvoker = null,");
            }

            // Hooks
            //GenerateHooks(writer, testInfo);
            writer.AppendLine("Hooks = new TestHooks { BeforeClass = Array.Empty<HookMetadata>(), AfterClass = Array.Empty<HookMetadata>(), BeforeTest = Array.Empty<HookMetadata>(), AfterTest = Array.Empty<HookMetadata>() },");

            // Always provide MethodInfo for reflection fallback
            if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
            {
                // For generic methods or types, we need to get the method without specifying parameter types
                // because they may contain generic type parameters
                var typeForMethodLookup = testInfo.TypeSymbol.IsGenericType
                    ? testInfo.TypeSymbol.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                    : context.ClassName;
                // InstanceFactory and TestInvoker already set above
            }
            else
            {
                // InstanceFactory and TestInvoker already set above
            }

            // Source location
            writer.AppendLine($"FilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {testInfo.LineNumber},");

            // Generic type information
            GenerateGenericTypeInfo(writer, testInfo);

            // Generic method information
            GenerateGenericMethodInfo(writer, testInfo);
        }
        writer.AppendLine(");");
    }

    private static void GenerateGenericTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For generic methods, check if there are explicit instantiations via [GenerateGenericTest]
        var explicitInstantiations = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute")
            .ToList();
            
        if (explicitInstantiations.Any())
        {
            // Generate a test for each explicit instantiation
            foreach (var attr in explicitInstantiations)
            {
                GenerateExplicitGenericTestMetadata(writer, testInfo, attr);
            }
        }
        else
        {
            // For generic methods without explicit instantiations, generate base metadata
            // The runtime will need to handle instantiation
            GenerateTestMetadata(writer, testInfo);
        }
    }
    
    private static void GenerateExplicitGenericTestMetadata(CodeWriter writer, TestMethodMetadata testInfo, AttributeData genericTestAttr)
    {
        // Extract type arguments from the attribute
        var typeArgs = new List<ITypeSymbol>();
        
        foreach (var arg in genericTestAttr.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
            {
                typeArgs.Add(typeSymbol);
            }
            else if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var typeConstant in arg.Values)
                {
                    if (typeConstant.Value is ITypeSymbol arrayTypeSymbol)
                    {
                        typeArgs.Add(arrayTypeSymbol);
                    }
                }
            }
        }
        
        if (typeArgs.Count != testInfo.MethodSymbol.TypeParameters.Length)
        {
            // Type argument count mismatch - skip this instantiation
            return;
        }
        
        // Generate specialized test metadata for this generic instantiation
        var typeArgsDisplay = string.Join(", ", typeArgs.Select(t => t.ToDisplayString()));
        var testId = $"{testInfo.TypeSymbol.ToDisplayString()}.{testInfo.MethodSymbol.Name}<{typeArgsDisplay}>";
        
        writer.AppendLine("_allTests.Add(new TestMetadata");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"TestId = \"{testId}\",");
        writer.AppendLine($"TestName = \"{testInfo.MethodSymbol.Name}<{typeArgsDisplay}>\",");
        writer.AppendLine($"TestClassType = typeof({testInfo.TypeSymbol.ToDisplayString()}),");
        writer.AppendLine($"TestMethodName = \"{testInfo.MethodSymbol.Name}\",");
        
        // Generate generic type arguments
        writer.Append("GenericMethodTypeArguments = new Type[] { ");
        writer.Append(string.Join(", ", typeArgs.Select(t => $"typeof({t.ToDisplayString()})")));
        writer.AppendLine(" },");
        
        // Generate the rest of the metadata
        GenerateCategories(writer, testInfo);
        GenerateSkipStatus(writer, testInfo);
        GenerateTimeout(writer, testInfo);
        writer.AppendLine($"RetryCount = {GetRetryCount(testInfo)},");
        writer.AppendLine($"CanRunInParallel = {GetCanRunInParallel(testInfo).ToString().ToLower()},");
        
        // Dependencies
        writer.AppendLine("DependsOn = Array.Empty<string>(),");
        
        // Data sources
        GenerateDataSources(writer, testInfo);
        GenerateClassDataSources(writer, testInfo);
        GeneratePropertyDataSources(writer, testInfo);
        
        // Parameter info
        writer.AppendLine($"ParameterCount = {testInfo.MethodSymbol.Parameters.Length},");
        GenerateParameterTypes(writer, testInfo);
        
        // Factory and invoker - null for generic instantiations
        writer.AppendLine("InstanceFactory = null, // Generic instantiation handled at runtime");
        writer.AppendLine("TestInvoker = null, // Generic instantiation handled at runtime");
        
        // Hooks
        GenerateHooks(writer, testInfo);
        writer.AppendLine($"FilePath = @\"{testInfo.FilePath}\",");
        writer.AppendLine($"LineNumber = {testInfo.LineNumber},");
        GenerateGenericTypeInfo(writer, testInfo);
        GenerateGenericMethodInfo(writer, testInfo);
        
        writer.Unindent();
        writer.AppendLine("});");
    }
    
    private static void RegisterDataSourceFactories(CodeWriter writer, TestMethodMetadata testInfo, HashSet<string> registeredSources)
    {
        // Find all MethodDataSource attributes
        var methodDataSources = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
            
        foreach (var attr in methodDataSources)
        {
            if (attr.ConstructorArguments.Length >= 2)
            {
                var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                var memberName = attr.ConstructorArguments[1].Value?.ToString();
                
                if (sourceType != null && memberName != null)
                {
                    var key = $"{sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{memberName}";
                    if (registeredSources.Add(key))
                    {
                        // Find the data source member
                        var member = sourceType.GetMembers(memberName).FirstOrDefault();
                        if (member is IMethodSymbol || member is IPropertySymbol)
                        {
                            var safeName = GetSafeDataSourceName(sourceType, memberName);
                            writer.AppendLine($"TestDelegateStorage.RegisterDataSourceFactory(\"{key}\", {safeName}_Factory);");
                        }
                    }
                }
            }
        }
        
        // Also check class-level data sources
        var classDataSources = testInfo.TypeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ClassDataSourceAttribute");
            
        foreach (var attr in classDataSources)
        {
            if (attr.ConstructorArguments.Length >= 1)
            {
                var sourceType = attr.ConstructorArguments[0].Type as INamedTypeSymbol ?? testInfo.TypeSymbol;
                var memberName = attr.ConstructorArguments[0].Value?.ToString();
                
                if (memberName != null)
                {
                    var key = $"{sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{memberName}";
                    if (registeredSources.Add(key))
                    {
                        var member = sourceType.GetMembers(memberName).FirstOrDefault();
                        if (member is IMethodSymbol || member is IPropertySymbol)
                        {
                            var safeName = GetSafeDataSourceName(sourceType, memberName);
                            writer.AppendLine($"TestDelegateStorage.RegisterDataSourceFactory(\"{key}\", {safeName}_Factory);");
                        }
                    }
                }
            }
        }
    }
    
    private static string GetSafeDataSourceName(ITypeSymbol type, string memberName)
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "_");
        
        return $"{typeName}_{memberName}";
    }



    private static void GenerateStaticDataSourceForArguments(CodeWriter writer, AttributeData argumentsAttribute)
    {
        writer.Append("new StaticTestDataSource(new object?[][] { new object?[] { ");

        var args = new List<string>();
        foreach (var arg in argumentsAttribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                foreach (var item in arg.Values)
                {
                    args.Add(FormatAttributeArgument(item.Value));
                }
            }
            else
            {
                args.Add(FormatAttributeArgument(arg.Value));
            }
        }

        writer.Append(string.Join(", ", args));
        writer.AppendLine(" } })");
    }

    private static ITypeSymbol[]? ExtractArgumentTypes(AttributeData attributeData)
    {
        var types = new List<ITypeSymbol>();

        foreach (var arg in attributeData.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                foreach (var item in arg.Values)
                {
                    var type = GetTypeFromValue(item);
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }
            }
            else
            {
                var type = GetTypeFromValue(arg);
                if (type != null)
                {
                    types.Add(type);
                }
            }
        }

        return types.ToArray();
    }

    private static ITypeSymbol? GetTypeFromValue(TypedConstant constant)
    {
        if (constant.Type == null)
        {
            return null;
        }

        // For typed constants, the Type property gives us the actual type
        return constant.Type;
    }

    private static void GenerateTestClassHelpers(CodeWriter writer, INamedTypeSymbol classSymbol, List<TestMethodMetadata> testMethods, DiagnosticContext diagnosticContext, TUnitConfiguration configuration)
    {
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var safeClassName = className.Replace(".", "_").Replace("<", "_").Replace(">", "_");

        writer.AppendLine($"#region Helpers for {className}");
        writer.AppendLine();

        // Track generated method names to avoid duplicates
        var generatedMethods = new HashSet<string>();

        // Generate test invokers for AOT
        foreach (var testInfo in testMethods)
        {
            var context = TestMetadataGenerationContext.Create(testInfo);
            if (context.CanUseStaticDefinition)
            {
                // Generate test invoker
                var methodName = $"{context.SafeClassName}_{context.SafeMethodName}_Invoker";
                if (!generatedMethods.Contains(methodName))
                {
                    generatedMethods.Add(methodName);
                    GenerateTestInvoker(writer, testInfo, context);
                }

                // Generate instance factory for parameterized constructors
                if (!context.HasParameterlessConstructor && context.ConstructorWithParameters != null)
                {
                    var factoryName = $"{context.SafeClassName}_InstanceFactory";
                    if (!generatedMethods.Contains(factoryName))
                    {
                        generatedMethods.Add(factoryName);
                        GenerateInstanceFactory(writer, testInfo, context);
                    }
                }
            }
        }

        // Generate data source factories using the new generator
        GenerateDataSourceFactoriesV2(writer, classSymbol, testMethods, configuration);
        
        // Generate property injection (always enabled)
        var hasPropertyInjection = GeneratePropertyInjection(writer, classSymbol, diagnosticContext);
        
        // Generate hook invokers (including property injection hooks if needed)
        GenerateHookInvokers(writer, classSymbol, hasPropertyInjection, configuration);

        writer.AppendLine($"#endregion");
        writer.AppendLine();
    }

    private static void GenerateTestInvoker(CodeWriter writer, TestMethodMetadata testInfo, TestMetadataGenerationContext context)
    {
        // Skip generating invokers for generic methods - they'll use reflection
        if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
        {
            return;
        }

        var methodName = $"{context.SafeClassName}_{context.SafeMethodName}_Invoker";

        using (writer.BeginBlock($"private static async Task {methodName}(object instance, object?[] args)"))
        {
            writer.AppendLine($"var typedInstance = ({context.ClassName})instance;");

            // Generate parameter extraction
            for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
            {
                var param = testInfo.MethodSymbol.Parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Handle nullable reference types in casts
                writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
            }

            // Generate method call
            var argList = string.Join(", ", Enumerable.Range(0, testInfo.MethodSymbol.Parameters.Length).Select(i => $"arg{i}"));
            var methodCall = $"typedInstance.{context.MethodName}({argList})";

            if (IsAsyncMethod(testInfo.MethodSymbol))
            {
                writer.AppendLine($"await {methodCall};");
            }
            else
            {
                writer.AppendLine($"{methodCall};");
            }
        }
        writer.AppendLine();
    }

    private static void GenerateInstanceFactory(CodeWriter writer, TestMethodMetadata testInfo, TestMetadataGenerationContext context)
    {
        if (context.ConstructorWithParameters == null)
        {
            return;
        }

        // Skip instance factory generation for classes with property data sources
        // These should be handled by the runtime TestBuilder with property injection
        var hasPropertyDataSources = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true));

        if (hasPropertyDataSources || context.RequiredProperties.Count > 0)
        {
            // Let the TestBuilder handle property injection at runtime
            return;
        }

        var factoryName = $"{context.SafeClassName}_InstanceFactory";
        var constructor = context.ConstructorWithParameters;

        using (writer.BeginBlock($"private static object {factoryName}(object?[] args)"))
        {
            // Validate argument count
            writer.AppendLine($"if (args == null || args.Length != {constructor.Parameters.Length})");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"throw new ArgumentException(\"Expected {constructor.Parameters.Length} arguments for {context.ClassName} constructor, but got \" + (args?.Length ?? 0));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();

            // Generate parameter extraction with type checking
            for (int i = 0; i < constructor.Parameters.Length; i++)
            {
                var param = constructor.Parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var paramName = $"arg{i}";

                // Generate null check for non-nullable value types
                if (param.Type.IsValueType && param.NullableAnnotation != NullableAnnotation.Annotated)
                {
                    writer.AppendLine($"if (args[{i}] == null)");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"throw new ArgumentException(\"Argument at index {i} cannot be null for parameter '{param.Name}' of type {paramType}\");");
                    writer.Unindent();
                    writer.AppendLine("}");
                }

                writer.AppendLine($"var {paramName} = ({paramType})args[{i}]!;");
            }

            // Generate constructor call
            var argList = string.Join(", ", Enumerable.Range(0, constructor.Parameters.Length).Select(i => $"arg{i}"));
            writer.AppendLine($"return new {context.ClassName}({argList});");
        }
        writer.AppendLine();
    }

    private static void GenerateDataSourceFactories(CodeWriter writer, INamedTypeSymbol classSymbol, List<TestMethodMetadata> testMethods, HashSet<string> generatedMethods)
    {
        var dataSourceMembers = new HashSet<(ITypeSymbol Type, string MemberName)>();
        
        // Collect all unique data source members from test methods
        foreach (var testInfo in testMethods)
        {
            // Method data sources
            var methodDataSources = testInfo.MethodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
                
            foreach (var attr in methodDataSources)
            {
                if (attr.ConstructorArguments.Length >= 2)
                {
                    var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                    var memberName = attr.ConstructorArguments[1].Value?.ToString();
                    
                    if (sourceType != null && memberName != null)
                    {
                        dataSourceMembers.Add((sourceType, memberName));
                    }
                }
            }
            
            // Class data sources
            var classDataSources = testInfo.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ClassDataSourceAttribute");
                
            foreach (var attr in classDataSources)
            {
                if (attr.ConstructorArguments.Length >= 1)
                {
                    var sourceType = attr.ConstructorArguments[0].Type as INamedTypeSymbol ?? testInfo.TypeSymbol;
                    var memberName = attr.ConstructorArguments[0].Value?.ToString();
                    
                    if (memberName != null)
                    {
                        dataSourceMembers.Add((sourceType, memberName));
                    }
                }
            }
        }
        
        // Generate factory methods for each unique data source
        foreach (var (sourceType, memberName) in dataSourceMembers)
        {
            var member = sourceType.GetMembers(memberName).FirstOrDefault();
            if (member != null)
            {
                var safeName = GetSafeDataSourceName(sourceType, memberName);
                var factoryName = $"{safeName}_Factory";
                
                if (!generatedMethods.Contains(factoryName))
                {
                    generatedMethods.Add(factoryName);
                    GenerateDataSourceFactory(writer, sourceType, member);
                }
            }
        }
    }
    
    private static void GenerateDataSourceFactory(CodeWriter writer, ITypeSymbol sourceType, ISymbol member)
    {
        var safeName = GetSafeDataSourceName(sourceType, member.Name);
        var typeName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        using (writer.BeginBlock($"private static IEnumerable<object?[]> {safeName}_Factory()"))
        {
            if (member is IPropertySymbol property)
            {
                writer.AppendLine($"var value = {typeName}.{member.Name};");
                writer.AppendLine("return ConvertToDataSourceArrays(value);");
            }
            else if (member is IMethodSymbol method)
            {
                if (method.IsAsync || method.ReturnType.Name.Contains("Task"))
                {
                    writer.AppendLine($"var task = {typeName}.{member.Name}();");
                    writer.AppendLine("var value = task.GetAwaiter().GetResult();");
                    writer.AppendLine("return ConvertToDataSourceArrays(value);");
                }
                else
                {
                    writer.AppendLine($"var value = {typeName}.{member.Name}();");
                    writer.AppendLine("return ConvertToDataSourceArrays(value);");
                }
            }
        }
        writer.AppendLine();
    }
    
    private static void GenerateConversionHelper(CodeWriter writer)
    {
        // Generate helper method for converting data source values
        using (writer.BeginBlock("private static IEnumerable<object?[]> ConvertToDataSourceArrays(object? value)"))
        {
            writer.AppendLine("if (value is IEnumerable<object?[]> arrays) return arrays;");
            writer.AppendLine();
            writer.AppendLine("if (value is System.Collections.IEnumerable enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var result = new List<object?[]>();");
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("if (item is object?[] array) result.Add(array);");
            writer.AppendLine("else if (item is System.Runtime.CompilerServices.ITuple tuple)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var tupleArray = new object?[tuple.Length];");
            writer.AppendLine("for (int i = 0; i < tuple.Length; i++) tupleArray[i] = tuple[i];");
            writer.AppendLine("result.Add(tupleArray);");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else result.Add(new[] { item });");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("return result;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
            writer.AppendLine("return new[] { new[] { value } };");
        }
        writer.AppendLine();
    }
    
    private static void GenerateHookInvokers(CodeWriter writer, INamedTypeSymbol classSymbol, bool hasPropertyInjection, TUnitConfiguration configuration)
    {
        // Find all hook methods in the class hierarchy
        var beforeClassHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: true);
        var afterClassHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: true);
        var beforeTestHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: false);
        var afterTestHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: false);

        // TODO: Generate standard hook invokers for user-defined hooks
        // For now, this method is just a placeholder for future hook generation
    }
    
    private static void RegisterPropertyInjectionHooks(CodeWriter writer, List<TestMethodMetadata> validTests, TUnitConfiguration configuration)
    {
        // Property injection is always enabled (no longer configurable)
            
        // Group tests by class to avoid duplicate registrations
        var testClasses = validTests.GroupBy(t => t.TypeSymbol, SymbolEqualityComparer.Default);
        
        foreach (var classGroup in testClasses)
        {
            if (classGroup.Key is INamedTypeSymbol classSymbol)
            {
                var safeClassName = classSymbol.Name.Replace(".", "_");
                
                // Check if this class has properties with [Inject] attribute
                var hasPropertyInjection = classSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "InjectAttribute"));
                    
                if (hasPropertyInjection)
                {
                    writer.AppendLine($"HookDelegateStorage.RegisterHook(\"{safeClassName}_InjectProperties\", {safeClassName}_InjectProperties);");
                    
                    // Check if disposal hook is needed
                    var hasDisposableProperties = classSymbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "InjectAttribute"))
                        .Any(p => p.Type.AllInterfaces.Any(i => i.Name == "IAsyncDisposable" || i.Name == "IDisposable"));
                        
                    if (hasDisposableProperties)
                    {
                        writer.AppendLine($"HookDelegateStorage.RegisterHook(\"{safeClassName}_DisposeProperties\", {safeClassName}_DisposeProperties);");
                    }
                }
            }
        }
    }

    private static void GenerateCategories(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var categories = GetCategories(testInfo);
        if (categories.Any())
        {
            writer.Append("Categories = new[] { ");
            writer.Append(string.Join(", ", categories.Select(c => $"\"{c}\"")));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("Categories = Array.Empty<string>(),");
        }
    }

    private static void GenerateSkipStatus(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var skipAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SkipAttribute");

        if (skipAttribute != null)
        {
            writer.AppendLine("IsSkipped = true,");
            var reason = skipAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "No reason provided";
            writer.AppendLine($"SkipReason = \"{reason}\",");
        }
        else
        {
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
        }
    }

    private static void GenerateTimeout(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var timeoutAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TimeoutAttribute");

        if (timeoutAttribute != null && timeoutAttribute.ConstructorArguments.Length > 0)
        {
            var timeout = timeoutAttribute.ConstructorArguments[0].Value;
            writer.AppendLine($"TimeoutMs = {timeout},");
        }
        else
        {
            writer.AppendLine("TimeoutMs = null,");
        }
    }

    private static void GenerateDependencies(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var dependsOnAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "DependsOnAttribute");

        var dependencies = new List<string>();
        foreach (var attr in dependsOnAttributes)
        {
            try
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    // DependsOn can have either:
                    // 1. Just a method name (string) - assumes same class
                    // 2. A type and method name (Type, string)
                    if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string methodName)
                    {
                        // Method in same class
                        dependencies.Add($"{testInfo.TypeSymbol.ToDisplayString()}.{methodName}");
                    }
                    else if (attr.ConstructorArguments.Length >= 2)
                    {
                        var depType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                        var depMethod = attr.ConstructorArguments[1].Value?.ToString();
                        if (depType != null && depMethod != null)
                        {
                            dependencies.Add($"{depType.ToDisplayString()}.{depMethod}");
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore attributes we can't process - they might have complex parameters
                continue;
            }
        }

        if (dependencies.Any())
        {
            writer.Append("DependsOn = new[] { ");
            writer.Append(string.Join(", ", dependencies.Select(d => $"\"{d}\"")));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("DependsOn = Array.Empty<string>(),");
        }
    }

    private static void GenerateDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For now, we'll only handle simple data sources here
        // AsyncDataSourceGenerator attributes are handled by the old source generation pipeline
        // and should be excluded from UnifiedTestMetadataGenerator

        var dataSources = new List<string>();

        // Arguments attributes
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");

        foreach (var attr in argumentsAttributes)
        {
            dataSources.Add($"new StaticTestDataSource(new object?[][] {{ new object?[] {{ {FormatAttributeArguments(attr)} }} }})");
        }

        // MethodDataSource attributes
        var methodDataAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

        foreach (var attr in methodDataAttributes)
        {
            var dataSource = GenerateDynamicDataSourceString(attr, testInfo.TypeSymbol);
            if (!string.IsNullOrEmpty(dataSource))
            {
                dataSources.Add(dataSource);
            }
        }

        // Note: AsyncDataSourceGenerator attributes are filtered out in GetTestMethodMetadata
        // so we shouldn't see them here

        if (dataSources.Any())
        {
            writer.AppendLine("DataSources = new TestDataSource[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var ds in dataSources)
            {
                writer.AppendLine($"{ds},");
            }
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        }
    }

    private static void GenerateClassDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Get class-level data source attributes
        var classDataSources = new List<string>();

        // Arguments attributes on class
        var argumentsAttributes = testInfo.TypeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");

        foreach (var attr in argumentsAttributes)
        {
            classDataSources.Add($"new StaticTestDataSource(new object?[][] {{ new object?[] {{ {FormatAttributeArguments(attr)} }} }})");
        }

        // MethodDataSource attributes on class
        var methodDataAttributes = testInfo.TypeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

        foreach (var attr in methodDataAttributes)
        {
            var dataSource = GenerateDynamicDataSourceString(attr, testInfo.TypeSymbol);
            if (!string.IsNullOrEmpty(dataSource))
            {
                classDataSources.Add(dataSource);
            }
        }

        if (classDataSources.Any())
        {
            writer.AppendLine("ClassDataSources = new TestDataSource[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var ds in classDataSources)
            {
                writer.AppendLine($"{ds},");
            }
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        }
    }

    private static void GeneratePropertyDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Find properties with data source attributes
        var properties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true));

        if (properties.Any())
        {
            writer.AppendLine("PropertyDataSources = new PropertyDataSource[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var prop in properties)
            {
                var dataSourceAttr = prop.GetAttributes().First(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true);
                writer.AppendLine("new PropertyDataSource");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"PropertyName = \"{prop.Name}\",");
                writer.AppendLine($"PropertyType = typeof({prop.Type.ToDisplayString()}),");

                // TODO: Generate proper data source based on attribute type
                // For now, skip property data sources
                writer.AppendLine("DataSource = new StaticTestDataSource(new object?[][] { new object?[] { null } })");
                writer.Unindent();
                writer.AppendLine("},");
            }

            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        }
    }

    private static void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For generic methods or types, we can't generate parameter types at compile time
        if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
        {
            writer.AppendLine("ParameterTypes = Array.Empty<Type>(), // Generic types resolved at runtime");
            return;
        }

        if (testInfo.MethodSymbol.Parameters.Length > 0)
        {
            writer.Append("ParameterTypes = new Type[] { ");
            var types = testInfo.MethodSymbol.Parameters.Select(p =>
            {
                var typeName = p.Type.ToDisplayString();
                // Remove nullable annotations for typeof()
                typeName = typeName.Replace("?", "");
                return $"typeof({typeName})";
            });
            writer.Append(string.Join(", ", types));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("ParameterTypes = Array.Empty<Type>(),");
        }
    }


    private static void GenerateHooks(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Check if this test class has properties with [Inject] attribute
        var hasPropertyInjection = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "InjectAttribute"));

        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();

        // Generate each hook type
        writer.AppendLine("BeforeClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterClass = Array.Empty<HookMetadata>(),");
        
        if (hasPropertyInjection)
        {
            // Add property injection as a before test hook
            var safeClassName = testInfo.TypeSymbol.Name.Replace(".", "_");
            writer.AppendLine("BeforeTest = new HookMetadata[]");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("new HookMetadata");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"Name = \"{safeClassName}_InjectProperties\",");
            writer.AppendLine("Order = -1000,"); // Run before user hooks
            writer.AppendLine("IsAsync = true,");
            writer.AppendLine("ReturnsValueTask = false,");
            writer.AppendLine($"HookInvoker = HookDelegateStorage.GetHook(\"{safeClassName}_InjectProperties\")");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("},");
            
            // TODO: Add property disposal as an after test hook if any properties implement IAsyncDisposable
            writer.AppendLine("AfterTest = Array.Empty<HookMetadata>()");
        }
        else
        {
            writer.AppendLine("BeforeTest = Array.Empty<HookMetadata>(),");
            writer.AppendLine("AfterTest = Array.Empty<HookMetadata>()");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static string GenerateDynamicDataSourceString(AttributeData attr, ITypeSymbol? containingType = null)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return string.Empty;
        }

        ITypeSymbol? sourceType = null;
        string? memberName = null;

        // Check if first argument is a type (two-argument constructor)
        if (attr.ConstructorArguments.Length >= 2)
        {
            sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            memberName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length == 1)
        {
            // Single argument - method name on the same class
            memberName = attr.ConstructorArguments[0].Value?.ToString();
            sourceType = containingType;
        }

        if (sourceType == null || memberName == null)
        {
            return string.Empty;
        }

        var isShared = attr.NamedArguments.FirstOrDefault(a => a.Key == "IsShared").Value.Value as bool? ?? true;
        
        // Check if the data source method is async
        var member = sourceType.GetMembers(memberName).FirstOrDefault();
        bool isAsync = false;
        
        if (member is IMethodSymbol methodSymbol)
        {
            isAsync = IsAsyncMethod(methodSymbol);
        }
        else if (member is IPropertySymbol propertySymbol)
        {
            isAsync = propertySymbol.Type.Name == "Task" || 
                     propertySymbol.Type.Name == "ValueTask" ||
                     propertySymbol.Type.AllInterfaces.Any(i => i.Name == "IAsyncEnumerable");
        }
        
        // Use appropriate data source type
        var dataSourceType = isAsync ? "AsyncDynamicTestDataSource" : "DynamicTestDataSource";
        
        // Note: Arguments are now handled by the data source factory, not stored in the metadata
        return $"new {dataSourceType}({isShared.ToString().ToLower()}) {{ FactoryKey = \"{sourceType.ToDisplayString()}.{memberName}\" }}";
    }

    private static void GenerateDynamicDataSource(CodeWriter writer, AttributeData attr)
    {
        // Extract source type and member name from attribute
        if (attr.ConstructorArguments.Length >= 2)
        {
            var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            var memberName = attr.ConstructorArguments[1].Value?.ToString();
            var isShared = attr.ConstructorArguments.Length > 2 ? attr.ConstructorArguments[2].Value?.ToString() ?? "false" : "false";

            if (sourceType != null && memberName != null)
            {
                writer.AppendLine($"new DynamicTestDataSource({isShared})");
                writer.AppendLine("{");
                writer.Indent();
                
                writer.AppendLine($"FactoryKey = \"{sourceType.ToDisplayString()}.{memberName}\"");

                writer.Unindent();
                writer.AppendLine("},");
            }
        }
    }

    private static string FormatAttributeArguments(AttributeData attr)
    {
        if (attr?.ConstructorArguments == null)
        {
            return string.Empty;
        }

        var args = new List<string>();
        foreach (var arg in attr.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                if (arg.Values != null)
                {
                    foreach (var item in arg.Values)
                    {
                        if (item.Kind == TypedConstantKind.Array)
                        {
                            // Nested array - handle separately
                            args.Add(FormatTypedConstant(item));
                        }
                        else
                        {
                            args.Add(FormatAttributeArgument(item.Value));
                        }
                    }
                }
            }
            else
            {
                args.Add(FormatTypedConstant(arg));
            }
        }
        return string.Join(", ", args);
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            var items = constant.Values.Select(v => FormatTypedConstant(v));
            return $"new[] {{ {string.Join(", ", items)} }}";
        }
        return FormatAttributeArgument(constant.Value);
    }

    private static string FormatAttributeArgument(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"@\"{s.Replace("\"", "\"\"")}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            ITypeSymbol type => $"typeof({type.ToDisplayString()})",
            _ => value.ToString() ?? "null"
        };
    }

    private static List<string> GetCategories(TestMethodMetadata testInfo)
    {
        var categories = new List<string>();

        var categoryAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "CategoryAttribute");

        foreach (var attr in categoryAttributes)
        {
            if (attr.ConstructorArguments.Length > 0)
            {
                var category = attr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(category))
                {
                    categories.Add(category!);
                }
            }
        }

        return categories;
    }

    private static int GetRetryCount(TestMethodMetadata testInfo)
    {
        var repeatAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");

        if (repeatAttribute != null && repeatAttribute.ConstructorArguments.Length > 0)
        {
            return (int) (repeatAttribute.ConstructorArguments[0].Value ?? 0);
        }

        return 0;
    }

    private static bool GetCanRunInParallel(TestMethodMetadata testInfo)
    {
        var notInParallelAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NotInParallelAttribute");

        return notInParallelAttribute == null;
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask" ||
               (returnType is INamedTypeSymbol namedType && namedType.IsGenericType &&
                (namedType.ConstructedFrom.Name == "Task" || namedType.ConstructedFrom.Name == "ValueTask"));
    }

    private static List<IMethodSymbol> FindHooksInHierarchy(INamedTypeSymbol type, string attributeName, bool isStatic)
    {
        var hooks = new List<IMethodSymbol>();

        var currentType = type;
        while (currentType != null)
        {
            var methods = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic == isStatic &&
                           m.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName));

            hooks.AddRange(methods);
            currentType = currentType.BaseType;
        }

        return hooks;
    }

    private static void GenerateGenericTypeInfo(CodeWriter writer, TestMethodMetadata testInfo)
    {
        if (!testInfo.TypeSymbol.IsGenericType)
        {
            writer.AppendLine("GenericTypeInfo = null,");
            return;
        }

        var typeParams = testInfo.TypeSymbol.TypeParameters;
        if (typeParams.Length == 0)
        {
            writer.AppendLine("GenericTypeInfo = null,");
            return;
        }

        using (writer.BeginBlock("GenericTypeInfo = new GenericTypeInfo"))
        {
            // Parameter names
            writer.Append("ParameterNames = new[] { ");
            writer.Append(string.Join(", ", typeParams.Select(p => $"\"{p.Name}\"")));
            writer.AppendLine(" },");

            // Constraints
            using (writer.BeginBlock("Constraints = new GenericParameterConstraints[]"))
            {
                for (int i = 0; i < typeParams.Length; i++)
                {
                    GenerateGenericParameterConstraints(writer, typeParams[i]);
                    if (i < typeParams.Length - 1)
                    {
                        writer.AppendLine(",");
                    }
                }
            }
        }
        writer.AppendLine(",");
    }

    private static void GenerateGenericMethodInfo(CodeWriter writer, TestMethodMetadata testInfo)
    {
        if (!testInfo.MethodSymbol.IsGenericMethod)
        {
            writer.AppendLine("GenericMethodInfo = null");
            return;
        }

        var typeParams = testInfo.MethodSymbol.TypeParameters;
        if (typeParams.Length == 0)
        {
            writer.AppendLine("GenericMethodInfo = null");
            return;
        }

        using (writer.BeginBlock("GenericMethodInfo = new GenericMethodInfo"))
        {
            // Parameter names
            writer.Append("ParameterNames = new[] { ");
            writer.Append(string.Join(", ", typeParams.Select(p => $"\"{p.Name}\"")));
            writer.AppendLine(" },");

            // Constraints
            using (writer.BeginBlock("Constraints = new GenericParameterConstraints[]"))
            {
                for (int i = 0; i < typeParams.Length; i++)
                {
                    GenerateGenericParameterConstraints(writer, typeParams[i]);
                    if (i < typeParams.Length - 1)
                    {
                        writer.AppendLine(",");
                    }
                }
            }
            writer.AppendLine(",");

            // Parameter positions - map which method parameters use which generic types
            var positions = new List<int>();
            for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
            {
                var paramType = testInfo.MethodSymbol.Parameters[i].Type;
                if (paramType is ITypeParameterSymbol typeParam)
                {
                    var index = Array.IndexOf(typeParams.ToArray(), typeParam);
                    if (index >= 0)
                    {
                        positions.Add(i);
                    }
                }
            }

            if (positions.Any())
            {
                writer.Append("ParameterPositions = new[] { ");
                writer.Append(string.Join(", ", positions));
                writer.AppendLine(" }");
            }
            else
            {
                writer.AppendLine("ParameterPositions = Array.Empty<int>()");
            }
        }
    }

    private static void GenerateGenericParameterConstraints(CodeWriter writer, ITypeParameterSymbol param)
    {
        using (writer.BeginBlock("new GenericParameterConstraints"))
        {
            writer.AppendLine($"ParameterName = \"{param.Name}\",");

            // Base type constraint
            var baseConstraint = param.ConstraintTypes.FirstOrDefault(t => t.TypeKind == TypeKind.Class);
            if (baseConstraint != null)
            {
                writer.AppendLine($"BaseTypeConstraint = typeof({baseConstraint.ToDisplayString()}),");
            }
            else
            {
                writer.AppendLine("BaseTypeConstraint = null,");
            }

            // Interface constraints
            var interfaces = param.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface).ToList();
            if (interfaces.Any())
            {
                writer.Append("InterfaceConstraints = new Type[] { ");
                writer.Append(string.Join(", ", interfaces.Select(i => $"typeof({i.ToDisplayString()})")));
                writer.AppendLine(" },");
            }
            else
            {
                writer.AppendLine("InterfaceConstraints = Array.Empty<Type>(),");
            }

            // Special constraints
            writer.AppendLine($"HasDefaultConstructorConstraint = {param.HasConstructorConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasReferenceTypeConstraint = {param.HasReferenceTypeConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasValueTypeConstraint = {param.HasValueTypeConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasNotNullConstraint = {param.HasNotNullConstraint.ToString().ToLower()}");
        }
    }

    private static bool IsTestInheritedByDerivedClass(IMethodSymbol methodSymbol, INamedTypeSymbol containingType)
    {
        // If this test is defined in a class that has derived classes with [InheritsTests],
        // skip it to avoid duplicate registration

        // Can't determine at compile time if there are derived classes in the current compilation context
        // This would require a more complex analysis across the compilation
        // For now, return false to maintain existing behavior

        // A better approach would be to have the InheritsTestsGenerator skip creating metadata
        // for tests that are already handled by UnifiedTestMetadataGenerator
        return false;
    }
    
    private static void GenerateDataSourceFactoriesV2(CodeWriter writer, INamedTypeSymbol classSymbol, List<TestMethodMetadata> testMethods, TUnitConfiguration configuration)
    {
        var dataSources = new List<DataSourceInfo>();
        
        // Collect all data sources from test methods
        foreach (var testMethod in testMethods)
        {
            // Check method data sources
            var methodDataSources = testMethod.MethodSymbol.GetAttributes()
                .Where(attr => attr.AttributeClass?.Name == "MethodDataSourceAttribute")
                .Select(attr => ExtractDataSourceInfo(attr, testMethod.MethodSymbol))
                .Where(info => info != null);
                
            dataSources.AddRange(methodDataSources!);
            
            // Check property data sources
            var propertyDataSources = testMethod.MethodSymbol.GetAttributes()
                .Where(attr => attr.AttributeClass?.Name == "PropertyDataSourceAttribute")
                .Select(attr => ExtractPropertyDataSourceInfo(attr))
                .Where(info => info != null);
                
            dataSources.AddRange(propertyDataSources!);
        }
        
        if (dataSources.Any())
        {
            // No limits on data sources (unlimited for optimal performance)
            var generator = new DataSourceFactoryGenerator();
            var code = generator.GenerateDataSourceFactories(dataSources);
            writer.Append(code);
            
            // Report data source count if verbose diagnostics enabled
            if (configuration.EnableVerboseDiagnostics)
            {
                writer.AppendLine($"// Generated {dataSources.Count} data source factories (no limits)");
            }
        }
    }
    
    private static DataSourceInfo? ExtractDataSourceInfo(AttributeData attribute, IMethodSymbol testMethod)
    {
        if (attribute.ConstructorArguments.Length < 2)
            return null;
            
        var typeArg = attribute.ConstructorArguments[0];
        var methodNameArg = attribute.ConstructorArguments[1];
        
        if (typeArg.Value is not INamedTypeSymbol type || methodNameArg.Value is not string methodName)
            return null;
            
        var method = type.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
        if (method == null)
            return null;
            
        return new DataSourceInfo
        {
            ContainingTypeName = type.ToDisplayString(),
            SafeTypeName = type.ToDisplayString().Replace(".", "_").Replace("<", "_").Replace(">", "_"),
            MemberName = methodName,
            SafeMemberName = methodName.Replace(".", "_"),
            IsProperty = false,
            IsAsync = method.IsAsync || IsTaskLikeType(method.ReturnType),
            ReturnType = method.ReturnType,
            FactoryKey = $"{type.ToDisplayString()}.{methodName}",
            Parameters = method.Parameters.Select(p => new ParameterInfo
            {
                ParameterName = p.Name,
                ParameterType = p.Type.ToDisplayString(),
                HasDefaultValue = p.HasExplicitDefaultValue,
                DefaultValue = p.ExplicitDefaultValue,
                IsParams = p.IsParams,
                ElementType = p.IsParams ? p.Type is IArrayTypeSymbol array ? array.ElementType.ToDisplayString() : null : null
            }).ToList()
        };
    }
    
    private static void GenerateStronglyTypedDelegatesInline(CodeWriter writer, List<TestMethodMetadata> validTests, DiagnosticContext diagnosticContext)
    {
        writer.AppendLine();
        writer.AppendLine("// Strongly-typed test delegates for AOT compilation");
        
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Strongly-typed delegates for test invocation without boxing");
        writer.AppendLine("/// </summary>");
        
        using (writer.BeginBlock("internal static class StronglyTypedTestDelegates"))
        {
            var processedMethods = new HashSet<string>();
            var processedFactories = new HashSet<string>();
            var registrations = new List<string>();
            
            foreach (var testMethod in validTests)
            {
                var methodKey = GetMethodKey(testMethod);
                if (processedMethods.Contains(methodKey))
                    continue;
                    
                processedMethods.Add(methodKey);
                var registration = GenerateTestDelegateInline(writer, testMethod, diagnosticContext, processedFactories);
                if (!string.IsNullOrEmpty(registration))
                {
                    registrations.Add(registration);
                }
            }
            
            // Generate single static constructor to register all delegates
            if (registrations.Count > 0)
            {
                using (writer.BeginBlock("static StronglyTypedTestDelegates()"))
                {
                    foreach (var registration in registrations)
                    {
                        writer.AppendLine(registration);
                    }
                }
            }
        }
        
        writer.AppendLine();
    }

    private static void GenerateTypedPropertySettersInline(CodeWriter writer, List<TestMethodMetadata> validTests, DiagnosticContext diagnosticContext)
    {
        writer.AppendLine();
        writer.AppendLine("// Strongly-typed property setters for dependency injection");
        
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Strongly-typed property setters for dependency injection");
        writer.AppendLine("/// </summary>");
        
        using (writer.BeginBlock("internal static class TypedPropertySetters"))
        {
            // For now, empty - will implement when needed
            writer.AppendLine("// Property setters will be generated here when needed");
        }
        
        writer.AppendLine();
    }

    private static string GetMethodKey(TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.ToDisplayString();
        var methodName = testMethod.MethodSymbol.Name;
        var parameters = string.Join(",", testMethod.MethodSymbol.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{className}.{methodName}({parameters})";
    }

    private static string GenerateTestDelegateInline(CodeWriter writer, TestMethodMetadata testMethod, DiagnosticContext? diagnosticContext, HashSet<string> processedFactories)
    {
        // Skip generic methods - they can't have strongly-typed delegates generated at compile time
        if (testMethod.MethodSymbol.IsGenericMethod)
        {
            return string.Empty;
        }
        
        var className = testMethod.TypeSymbol.ToDisplayString();
        var methodName = testMethod.MethodSymbol.Name;
        var safeClassName = testMethod.TypeSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        var safeMethodName = methodName.Replace(".", "_");
        
        var parameters = testMethod.MethodSymbol.Parameters;
        var returnType = testMethod.MethodSymbol.ReturnType;
        
        // Determine if method is async
        var isAsync = IsAsyncMethod(testMethod.MethodSymbol);
        
        // Generate delegate type
        var delegateTypeName = $"{safeClassName}_{safeMethodName}_Delegate";
        
        // Build parameter list for delegate
        var parameterTypes = new List<string> { className }; // Instance type first
        parameterTypes.AddRange(parameters.Select(p => p.Type.ToDisplayString()));
        
        var returnTypeName = isAsync ? "Task" : "void";
        
        // Generate delegate type declaration
        writer.AppendLine($"/// <summary>");
        writer.AppendLine($"/// Strongly-typed delegate for {className}.{methodName}");
        writer.AppendLine($"/// </summary>");
        writer.Append($"public delegate {returnTypeName} {delegateTypeName}(");
        writer.Append(string.Join(", ", parameterTypes.Select((type, index) => 
            index == 0 ? $"{type} instance" : $"{type} arg{index}")));
        writer.AppendLine(");");
        writer.AppendLine();

        // Generate delegate instance
        writer.AppendLine($"/// <summary>");
        writer.AppendLine($"/// Strongly-typed delegate instance for {className}.{methodName}");
        writer.AppendLine($"/// </summary>");
        writer.Append($"public static readonly {delegateTypeName} {safeClassName}_{safeMethodName} = ");
        
        if (isAsync)
        {
            writer.Append("async ");
        }
        
        writer.Append("(");
        writer.Append(string.Join(", ", parameterTypes.Select((type, index) => 
            index == 0 ? "instance" : $"arg{index}")));
        writer.Append(") => ");
        
        if (isAsync)
        {
            writer.Append("await ");
        }
        
        writer.Append($"instance.{methodName}(");
        writer.Append(string.Join(", ", parameters.Select((p, index) => $"arg{index + 1}")));
        writer.AppendLine(");");
        writer.AppendLine();

        // Generate instance factory only once per class
        var factoryKey = $"{safeClassName}_Factory";
        if (!processedFactories.Contains(factoryKey))
        {
            processedFactories.Add(factoryKey);
            writer.AppendLine($"/// <summary>");
            writer.AppendLine($"/// Instance factory for {className} (parameterless constructor)");
            writer.AppendLine($"/// </summary>");
            writer.AppendLine($"public static readonly Func<{className}> {safeClassName}_Factory = () => new {className}();");
            writer.AppendLine();
        }

        // Return registration code for later inclusion in static constructor
        var typeArray = parameters.Length > 0 
            ? $"new[] {{ {string.Join(", ", parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))} }}"
            : "new Type[0]"; // Use explicit Type[0] for empty arrays
        var registrationCode = $"TestDelegateStorage.RegisterStronglyTypedDelegate(\"{className}.{methodName}\", {typeArray}, {safeClassName}_{safeMethodName});";
        
        writer.AppendLine();
        return registrationCode;
    }

    private static void GenerateModuleInitializerSource(SourceProductionContext context, List<TestMethodMetadata> validTests, DiagnosticContext diagnosticContext)
    {
        var generator = new ModuleInitializerGenerator();
        var moduleInitializerCode = generator.GenerateModuleInitializer(validTests, diagnosticContext);
        
        context.AddSource("TUnitModuleInitializer.g.cs", moduleInitializerCode);
    }

    private static DataSourceInfo? ExtractPropertyDataSourceInfo(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length < 2)
            return null;
            
        var typeArg = attribute.ConstructorArguments[0];
        var propertyNameArg = attribute.ConstructorArguments[1];
        
        if (typeArg.Value is not INamedTypeSymbol type || propertyNameArg.Value is not string propertyName)
            return null;
            
        var property = type.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
        if (property == null)
            return null;
            
        return new DataSourceInfo
        {
            ContainingTypeName = type.ToDisplayString(),
            SafeTypeName = type.ToDisplayString().Replace(".", "_").Replace("<", "_").Replace(">", "_"),
            MemberName = propertyName,
            SafeMemberName = propertyName.Replace(".", "_"),
            IsProperty = true,
            IsAsync = IsTaskLikeType(property.Type),
            ReturnType = property.Type,
            FactoryKey = $"{type.ToDisplayString()}.{propertyName}",
            Parameters = new List<ParameterInfo>()
        };
    }
    
    private static bool IsTaskLikeType(ITypeSymbol type)
    {
        var typeName = type.Name;
        return typeName == "Task" || typeName == "ValueTask" || 
               typeName == "IAsyncEnumerable" || type.AllInterfaces.Any(i => i.Name == "IAsyncEnumerable");
    }
    

    private static bool GeneratePropertyInjection(CodeWriter writer, INamedTypeSymbol classSymbol, DiagnosticContext? diagnosticContext = null)
    {
        var generator = new PropertyInjectionGenerator();
        var context = new PropertyInjectionContext
        {
            ClassSymbol = classSymbol,
            ClassName = classSymbol.ToDisplayString(),
            SafeClassName = classSymbol.Name.Replace(".", "_"),
            DiagnosticContext = diagnosticContext
        };
        
        var code = generator.GeneratePropertyInjection(context);
        if (!string.IsNullOrEmpty(code))
        {
            writer.Append(code);
            return true;
        }
        
        return false;
    }
}
