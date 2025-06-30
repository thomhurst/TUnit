using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Main orchestrator for test metadata generation following Single Responsibility Principle
/// </summary>
[Generator]
public sealed class UnifiedTestMetadataGeneratorV2 : IIncrementalGenerator
{
    private const string GeneratedNamespace = "TUnit.Generated";
    private const string RegistryClassName = "UnifiedTestMetadataRegistry";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register post-initialization output for base types
        context.RegisterPostInitializationOutput(GenerateBaseTypes);

        // Find all test methods
        var testMethodsProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsTestMethod(node),
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Find all generic test methods with explicit instantiations
        var genericTestsProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => HasGenerateGenericTestAttribute(node),
                transform: static (ctx, _) => GetGenericTestMetadata(ctx))
            .Where(static m => m is not null)
            .SelectMany(static (m, _) => m!);

        // Combine all test methods
        var allTestMethods = testMethodsProvider
            .Collect()
            .Combine(genericTestsProvider.Collect())
            .Select(static (combined, _) => combined.Left.Concat(combined.Right));

        // Generate the unified test registry
        context.RegisterSourceOutput(allTestMethods, GenerateTestRegistry);
    }

    private static bool IsTestMethod(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax method &&
               method.AttributeLists.Any(al => 
                   al.Attributes.Any(a => 
                       a.Name.ToString().EndsWith("Test") || 
                       a.Name.ToString().EndsWith("TestAttribute")));
    }

    private static bool HasGenerateGenericTestAttribute(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax classDecl)
        {
            return classDecl.AttributeLists.Any(al =>
                al.Attributes.Any(a =>
                    a.Name.ToString().Contains("GenerateGenericTest")));
        }

        if (node is MethodDeclarationSyntax methodDecl)
        {
            return methodDecl.AttributeLists.Any(al =>
                al.Attributes.Any(a =>
                    a.Name.ToString().Contains("GenerateGenericTest")));
        }

        return false;
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
        
        if (methodSymbol?.ContainingType == null)
            return null;

        var containingType = methodSymbol.ContainingType as INamedTypeSymbol;
        if (containingType == null)
            return null;

        // Skip abstract classes (cannot be instantiated)
        if (containingType.IsAbstract)
            return null;

        // Skip generic types without explicit instantiation
        if (containingType.IsGenericType && containingType.TypeParameters.Length > 0)
            return null;

        // Skip generic methods without explicit instantiation
        if (methodSymbol is IMethodSymbol method && method.IsGenericMethod)
            return null;

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol as IMethodSymbol ?? throw new InvalidOperationException("Symbol is not a method"),
            TypeSymbol = containingType,
            MethodSyntax = methodSyntax
        };
    }

    private static IEnumerable<TestMethodMetadata>? GetGenericTestMetadata(GeneratorSyntaxContext context)
    {
        var results = new List<TestMethodMetadata>();

        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return null;

            // Get all generic test methods in the class
            var namedTypeSymbol = classSymbol as INamedTypeSymbol;
            if (namedTypeSymbol == null) return null;
            
            // Skip abstract classes (cannot be instantiated)
            if (namedTypeSymbol.IsAbstract) return null;
            
            var genericMethods = namedTypeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsGenericMethod && HasTestAttribute(m));

            // Get GenerateGenericTest attributes
            var generateAttributes = classSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute");

            foreach (var attr in generateAttributes)
            {
                var typeArgs = ExtractTypeArguments(attr);
                if (typeArgs.Length == 0) continue;

                foreach (var method in genericMethods)
                {
                    if (method.TypeParameters.Length != typeArgs.Length) continue;

                    // Create a constructed generic method
                    var constructedMethod = method.Construct(typeArgs);
                    
                    results.Add(new TestMethodMetadata
                    {
                        MethodSymbol = constructedMethod,
                        TypeSymbol = namedTypeSymbol,
                        MethodSyntax = null, // Will need to handle this differently
                        GenericTypeArguments = typeArgs
                    });
                }
            }
        }

        return results.Any() ? results : null;
    }

    private static bool HasTestAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(a => 
            a.AttributeClass?.Name == "TestAttribute" ||
            a.AttributeClass?.Name == "Test");
    }

    private static ITypeSymbol[] ExtractTypeArguments(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return Array.Empty<ITypeSymbol>();

        var typeArgs = new List<ITypeSymbol>();
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
            {
                typeArgs.Add(typeSymbol);
            }
        }

        return typeArgs.ToArray();
    }

    private void GenerateBaseTypes(IncrementalGeneratorPostInitializationContext context)
    {
        // Generate base storage classes if they don't exist
        GenerateTestDelegateStorage(context);
        GenerateHookDelegateStorage(context);
        GenerateDataSourceFactoryRegistry(context);
        GenerateTypeArrayComparer(context);
    }

    private void GenerateTestDelegateStorage(IncrementalGeneratorPostInitializationContext context)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace TUnit.Core
            {
                /// <summary>
                /// Storage for strongly-typed test delegates
                /// </summary>
                public static class TestDelegateStorage
                {
                    private static readonly Dictionary<string, Func<object?[], object>> _instanceFactories = new();
                    private static readonly Dictionary<string, Func<object, object?[], Task>> _testInvokers = new();

                    public static void RegisterInstanceFactory(string key, Func<object?[], object> factory)
                    {
                        _instanceFactories[key] = factory;
                    }

                    public static void RegisterTestInvoker(string key, Func<object, object?[], Task> invoker)
                    {
                        _testInvokers[key] = invoker;
                    }

                    public static Func<object?[], object>? GetInstanceFactory(string key)
                    {
                        return _instanceFactories.TryGetValue(key, out var factory) ? factory : null;
                    }

                    public static Func<object, object?[], Task>? GetTestInvoker(string key)
                    {
                        return _testInvokers.TryGetValue(key, out var invoker) ? invoker : null;
                    }
                }
            }
            """;

        context.AddSource("TestDelegateStorage.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateHookDelegateStorage(IncrementalGeneratorPostInitializationContext context)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Core;

            namespace TUnit.Core
            {
                /// <summary>
                /// Storage for hook delegates
                /// </summary>
                public static class HookDelegateStorage
                {
                    private static readonly Dictionary<string, Func<object?, HookContext, Task>> _hooks = new();

                    public static void RegisterHook(string key, Func<object?, HookContext, Task> hook)
                    {
                        _hooks[key] = hook;
                    }

                    public static Func<object?, HookContext, Task>? GetHook(string key)
                    {
                        return _hooks.TryGetValue(key, out var hook) ? hook : null;
                    }
                }
            }
            """;

        context.AddSource("HookDelegateStorage.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateDataSourceFactoryRegistry(IncrementalGeneratorPostInitializationContext context)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;

            namespace TUnit.Core
            {
                /// <summary>
                /// Registry for data source factories
                /// </summary>
                public static class DataSourceFactoryRegistry
                {
                    private static readonly Dictionary<string, Func<CancellationToken, IEnumerable<object?[]>>> _factories = new();

                    public static void Register(string key, Func<CancellationToken, IEnumerable<object?[]>> factory)
                    {
                        _factories[key] = factory;
                    }

                    public static Func<CancellationToken, IEnumerable<object?[]>>? GetFactory(string key)
                    {
                        return _factories.TryGetValue(key, out var factory) ? factory : null;
                    }
                }
            }
            """;

        context.AddSource("DataSourceFactoryRegistry.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateTypeArrayComparer(IncrementalGeneratorPostInitializationContext context)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            namespace TUnit.Core
            {
                /// <summary>
                /// Equality comparer for arrays of Type objects used in generic type resolution
                /// </summary>
                internal sealed class TypeArrayComparer : IEqualityComparer<Type[]>
                {
                    public static readonly TypeArrayComparer Instance = new();

                    private TypeArrayComparer() { }

                    public bool Equals(Type[]? x, Type[]? y)
                    {
                        if (ReferenceEquals(x, y)) return true;
                        if (x is null || y is null) return false;
                        if (x.Length != y.Length) return false;

                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != y[i])
                                return false;
                        }

                        return true;
                    }

                    public int GetHashCode(Type[] obj)
                    {
                        // Use simple hash combining for .NET Standard 2.0 compatibility
                        int hash = 17;
                        foreach (var type in obj)
                        {
                            hash = hash * 31 + type.GetHashCode();
                        }
                        return hash;
                    }
                }
            }
            """;

        context.AddSource("TypeArrayComparer.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateTestRegistry(SourceProductionContext context, IEnumerable<TestMethodMetadata> testMethods)
    {
        try
        {
            var writer = new CodeWriter();
            
            // Generate file header
            GenerateFileHeader(writer);

            // Create generator instances
            var hookGenerator = new HookGenerator();
            var dataSourceGenerator = new DataSourceGenerator();
            var delegateGenerator = new DelegateGenerator();
            var genericTypeResolver = new GenericTypeResolver();
            var metadataGenerator = new MetadataGenerator(hookGenerator, dataSourceGenerator);
            
            // Note: Generic analysis would need to be done in a separate step
            // For now, we'll generate the registry structure without analysis

            // Generate the registry class
            writer.AppendLine($"public static class {RegistryClassName}");
            writer.AppendLine("{");
            writer.Indent();

            // Generate test list
            writer.AppendLine("private static readonly List<TestMetadata> _allTests = new();");
            writer.AppendLine();

            // Generate module initializer
            GenerateModuleInitializer(writer);

            // Generate registration methods
            GenerateRegisterAllDelegates(writer, delegateGenerator, hookGenerator, testMethods);
            GenerateRegisterDataSourceFactories(writer, dataSourceGenerator, testMethods);
            GenerateRegisterAllTests(writer, metadataGenerator, testMethods);

            // Generate generic test registry
            writer.AppendLine();
            writer.AppendLine("// Generic test registry");
            genericTypeResolver.GenerateGenericTestRegistry(writer);
            
            // Generate async data source wrapper methods
            writer.AppendLine();
            writer.AppendLine("// Async data source wrapper methods");
            dataSourceGenerator.GenerateAsyncDataSourceWrappers(writer, testMethods);

            // Generate helper methods that may be needed
            writer.AppendLine();
            writer.AppendLine("// Helper methods");
            GenerateHelperMethods(writer, dataSourceGenerator, testMethods);

            // Generate delegate implementations
            writer.AppendLine();
            writer.AppendLine("// Strongly-typed delegate implementations");
            delegateGenerator.GenerateStronglyTypedDelegates(writer, testMethods);

            // Generate hook implementations
            writer.AppendLine();
            writer.AppendLine("// Hook implementations");
            hookGenerator.GenerateHookInvokers(writer, testMethods);

            writer.Unindent();
            writer.AppendLine("}");

            // Add source
            var source = writer.ToString();
            context.AddSource($"{RegistryClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            // Report diagnostic
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TU0001",
                    "Test Generation Failed",
                    "Failed to generate test metadata: {0}",
                    "TUnit.Generation",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                ex.Message);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void GenerateFileHeader(CodeWriter writer)
    {
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#pragma warning disable");
        writer.AppendLine();
        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Runtime.CompilerServices;");
        writer.AppendLine("using System.Threading;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine("using global::TUnit.Core.Services;");
        writer.AppendLine($"namespace {GeneratedNamespace};");
    }

    private void GenerateModuleInitializer(CodeWriter writer)
    {
        writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        // Load referenced assemblies for complete test discovery
        writer.AppendLine("// Load referenced assemblies to ensure complete test discovery");
        writer.AppendLine("LoadReferencedAssemblies();");
        writer.AppendLine();
        
        // Register all generated metadata
        writer.AppendLine("// Register all generated metadata");
        writer.AppendLine("RegisterAllDelegates();");
        writer.AppendLine("RegisterDataSourceFactories();");
        writer.AppendLine("RegisterAllTests();");
        writer.AppendLine();
        
        // Create efficient test registry with proper equality comparers
        writer.AppendLine("// Register with the discovery service using efficient registry");
        writer.AppendLine("// Note: Registration will be handled by the test engine at runtime");
        writer.AppendLine();
        
        // Register generic test registry if available
        writer.AppendLine("// Register generic test combinations if any were discovered");
        writer.AppendLine("RegisterGenericTestCombinations();");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"Failed to initialize test registry: {ex}\");");
        writer.AppendLine("throw;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        
        // Generate assembly loading method
        GenerateAssemblyLoadingMethod(writer);
        
        // Generate generic test registration method
        GenerateGenericTestRegistrationMethod(writer);
    }

    private void GenerateRegisterAllDelegates(
        CodeWriter writer, 
        DelegateGenerator delegateGenerator, 
        HookGenerator hookGenerator,
        IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("private static void RegisterAllDelegates()");
        writer.AppendLine("{");
        writer.Indent();
        
        delegateGenerator.GenerateDelegateRegistrations(writer, testMethods);
        hookGenerator.GenerateHookRegistrations(writer, testMethods);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private void GenerateRegisterDataSourceFactories(
        CodeWriter writer,
        DataSourceGenerator dataSourceGenerator,
        IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("private static void RegisterDataSourceFactories()");
        writer.AppendLine("{");
        writer.Indent();
        
        dataSourceGenerator.GenerateDataSourceRegistrations(writer, testMethods);
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private void GenerateRegisterAllTests(
        CodeWriter writer,
        MetadataGenerator metadataGenerator,
        IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("private static void RegisterAllTests()");
        writer.AppendLine("{");
        writer.Indent();
        
        metadataGenerator.GenerateTestRegistrations(writer, testMethods);
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateHelperMethods(
        CodeWriter writer,
        DataSourceGenerator dataSourceGenerator, 
        IEnumerable<TestMethodMetadata> testMethods)
    {
        // Check if we need the ConvertToSync helper
        var hasAsyncDataSources = testMethods.Any(tm =>
        {
            var methodDataSources = tm.MethodSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
            var propertyDataSources = tm.TypeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "DataSourceForAttribute"));
            return methodDataSources || propertyDataSources;
        });

        if (hasAsyncDataSources)
        {
            GenerateConvertToSyncHelper(writer);
        }
    }

    private void GenerateConvertToSyncHelper(CodeWriter writer)
    {
        writer.AppendLine("private static IEnumerable<object?[]> ConvertToSync(Func<CancellationToken, IAsyncEnumerable<object?[]>> asyncFactory)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("var cts = new CancellationTokenSource();");
        writer.AppendLine("var enumerator = asyncFactory(cts.Token).GetAsyncEnumerator(cts.Token);");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("while (true)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (!enumerator.MoveNextAsync().AsTask().Wait(TimeSpan.FromSeconds(30)))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("break;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (AggregateException ae) when (ae.InnerException is OperationCanceledException)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("break;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("yield return enumerator.Current;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("finally");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("enumerator.DisposeAsync().AsTask().Wait();");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch { }");
        writer.AppendLine("cts.Dispose();");
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateAssemblyLoadingMethod(CodeWriter writer)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Loads referenced assemblies to ensure complete test discovery across modules");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("private static void LoadReferencedAssemblies()");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// AOT-only mode: All test metadata is statically generated at compile-time");
        writer.AppendLine("// No runtime assembly loading or reflection required");
        writer.AppendLine("Console.WriteLine(\"TUnit: Running in AOT-only mode with compile-time test discovery\");");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"Warning: Assembly loading failed: {ex.Message}\");");
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GenerateGenericTestRegistrationMethod(CodeWriter writer)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Registers generic test combinations discovered at compile-time");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("private static void RegisterGenericTestCombinations()");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// Check if GenericTestRegistry exists and has tests");
        writer.AppendLine("// This will be empty if no generic tests were discovered");
        writer.AppendLine("var genericTests = GenericTestRegistry.GetAllGenericTests();");
        writer.AppendLine("if (genericTests.Any())");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("foreach (var (typeArgs, metadata) in genericTests)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("_allTests.Add(metadata);");
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.AppendLine("Console.WriteLine($\"Registered {genericTests.Count()} generic test combinations\");");
        
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Generic test registration is optional - don't fail if it doesn't work");
        writer.AppendLine("Console.Error.WriteLine($\"Warning: Failed to register generic tests: {ex.Message}\");");
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
    }
}

/// <summary>
/// Metadata for a test method
/// </summary>
internal class TestMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public MethodDeclarationSyntax? MethodSyntax { get; init; }
    public ITypeSymbol[]? GenericTypeArguments { get; init; }
}