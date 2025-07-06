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

        // Find all test classes (classes that contain test methods)
        var testClassesProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsTestClass(node),
                transform: static (ctx, _) => GetTestClassMetadata(ctx))
            .Where(static m => m is not null);

        // Generate one source file per test class
        context.RegisterSourceOutput(testClassesProvider, GenerateTestClassSource!);
    }

    private static bool IsTestClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
        {
            return false;
        }

        // Check if the class has any test methods
        return classDecl.Members.OfType<MethodDeclarationSyntax>()
            .Any(method => method.AttributeLists.Any(al =>
                al.Attributes.Any(a =>
                    a.Name.ToString().EndsWith("Test") ||
                    a.Name.ToString().EndsWith("TestAttribute"))));
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

    private static bool IsGenericTestMethodOrClass(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax classDecl)
        {
            // Check if it's a generic class with test methods
            return classDecl.TypeParameterList != null &&
                   classDecl.Members.OfType<MethodDeclarationSyntax>()
                       .Any(m => m.AttributeLists.Any(al =>
                           al.Attributes.Any(a =>
                               a.Name.ToString().EndsWith("Test") ||
                               a.Name.ToString().EndsWith("TestAttribute"))));
        }

        if (node is MethodDeclarationSyntax methodDecl)
        {
            // Check if it's a generic test method
            return methodDecl.TypeParameterList != null &&
                   methodDecl.AttributeLists.Any(al =>
                       al.Attributes.Any(a =>
                           a.Name.ToString().EndsWith("Test") ||
                           a.Name.ToString().EndsWith("TestAttribute")));
        }

        return false;
    }

    private static TestClassGroup? GetTestClassMetadata(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        
        if (classSymbol == null || classSymbol.IsAbstract)
        {
            return null;
        }

        // Skip generic types without explicit instantiation
        if (classSymbol.IsGenericType && classSymbol.TypeParameters.Length > 0)
        {
            return null;
        }

        var testMethods = new List<TestMethodMetadata>();

        // Get all test methods in this class
        var methods = classSymbol.GetMembers().OfType<IMethodSymbol>();
        foreach (var method in methods)
        {
            // Skip generic methods for now
            if (method.IsGenericMethod)
            {
                continue;
            }

            // Check if it's a test method
            if (!HasTestAttribute(method))
            {
                continue;
            }

            // Find the corresponding syntax node
            var methodSyntax = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == method.Name);

            if (methodSyntax != null)
            {
                testMethods.Add(new TestMethodMetadata
                {
                    MethodSymbol = method,
                    TypeSymbol = classSymbol,
                    MethodSyntax = methodSyntax
                });
            }
        }

        if (testMethods.Count == 0)
        {
            return null;
        }

        return new TestClassGroup
        {
            TypeSymbol = classSymbol,
            TestMethods = testMethods
        };
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);

        if (methodSymbol?.ContainingType == null)
        {
            return null;
        }

        var containingType = methodSymbol.ContainingType as INamedTypeSymbol;
        if (containingType == null)
        {
            return null;
        }

        // Skip abstract classes (cannot be instantiated)
        if (containingType.IsAbstract)
        {
            return null;
        }

        // Skip generic types without explicit instantiation
        // Check for open generic types (types with unbound type parameters)
        if (containingType.IsGenericType && containingType.TypeParameters.Length > 0)
        {
            return null;
        }

        // Also check if any type arguments are type parameters (e.g., T)
        if (containingType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter))
            {
                return null;
            }
        }

        // Check the entire type hierarchy for unresolved type parameters
        var currentType = containingType.BaseType;
        while (currentType != null)
        {
            if (currentType.TypeKind == TypeKind.TypeParameter)
            {
                return null;
            }

            if (currentType is INamedTypeSymbol baseNamedType && baseNamedType.IsGenericType)
            {
                if (baseNamedType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter))
                {
                    return null;
                }
            }

            currentType = currentType.BaseType;
        }

        // Skip generic methods without explicit instantiation
        if (methodSymbol is IMethodSymbol method && method.IsGenericMethod)
        {
            return null;
        }

        // Also skip if method has parameters with unresolved type parameters
        if (methodSymbol is IMethodSymbol methodWithParams &&
            methodWithParams.Parameters.Any(p => ContainsTypeParameter(p.Type)))
        {
            return null;
        }

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
            if (classSymbol == null)
            {
                return null;
            }

            // Get all generic test methods in the class
            var namedTypeSymbol = classSymbol as INamedTypeSymbol;
            if (namedTypeSymbol == null)
            {
                return null;
            }

            // Skip abstract classes (cannot be instantiated)
            if (namedTypeSymbol.IsAbstract)
            {
                return null;
            }

            var genericMethods = namedTypeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsGenericMethod && HasTestAttribute(m));

            // Get GenerateGenericTest attributes
            var generateAttributes = classSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute");

            foreach (var attr in generateAttributes)
            {
                var typeArgs = ExtractTypeArguments(attr);
                if (typeArgs.Length == 0)
                {
                    continue;
                }

                foreach (var method in genericMethods)
                {
                    if (method.TypeParameters.Length != typeArgs.Length)
                    {
                        continue;
                    }

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

    private static IEnumerable<TestMethodMetadata>? GetInferredGenericTestMetadata(GeneratorSyntaxContext context)
    {
        var results = new List<TestMethodMetadata>();
        var resolver = new GenericTypeResolver();

        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (classSymbol == null || classSymbol.IsAbstract)
            {
                return null;
            }

            // Skip if explicit GenerateGenericTest attributes are present
            if (classSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute"))
            {
                return null;
            }

            // Analyze generic test methods
            var genericMethods = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsGenericMethod && HasTestAttribute(m));

            foreach (var method in genericMethods)
            {
                var inferredTypes = resolver.InferGenericTypesFromDataSources(method);

                foreach (var typeArgs in inferredTypes)
                {
                    if (typeArgs.Length == method.TypeParameters.Length)
                    {
                        var constructedMethod = method.Construct(typeArgs);
                        results.Add(new TestMethodMetadata
                        {
                            MethodSymbol = constructedMethod,
                            TypeSymbol = classSymbol,
                            MethodSyntax = null,
                            GenericTypeArguments = typeArgs
                        });
                    }
                }
            }

            // Handle generic classes
            if (classSymbol.IsGenericType)
            {
                var testMethods = classSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(HasTestAttribute);

                foreach (var method in testMethods)
                {
                    var inferredTypes = resolver.InferGenericTypesFromDataSources(method);

                    foreach (var typeArgs in inferredTypes)
                    {
                        if (typeArgs.Length == classSymbol.TypeParameters.Length)
                        {
                            var constructedClass = classSymbol.Construct(typeArgs);
                            var constructedMethod = constructedClass.GetMembers(method.Name)
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault();

                            if (constructedMethod != null)
                            {
                                results.Add(new TestMethodMetadata
                                {
                                    MethodSymbol = constructedMethod,
                                    TypeSymbol = constructedClass,
                                    MethodSyntax = null,
                                    GenericTypeArguments = typeArgs
                                });
                            }
                        }
                    }
                }
            }
        }
        else if (context.Node is MethodDeclarationSyntax methodDecl)
        {
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            if (methodSymbol?.ContainingType == null)
            {
                return null;
            }

            var containingType = methodSymbol.ContainingType as INamedTypeSymbol;
            if (containingType == null || containingType.IsAbstract)
            {
                return null;
            }

            // Skip if explicit GenerateGenericTest attributes are present
            if (methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute"))
            {
                return null;
            }

            if (methodSymbol.IsGenericMethod && HasTestAttribute(methodSymbol))
            {
                var inferredTypes = resolver.InferGenericTypesFromDataSources(methodSymbol);

                foreach (var typeArgs in inferredTypes)
                {
                    if (typeArgs.Length == methodSymbol.TypeParameters.Length)
                    {
                        var constructedMethod = methodSymbol.Construct(typeArgs);
                        results.Add(new TestMethodMetadata
                        {
                            MethodSymbol = constructedMethod,
                            TypeSymbol = containingType,
                            MethodSyntax = methodDecl,
                            GenericTypeArguments = typeArgs
                        });
                    }
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
        {
            return Array.Empty<ITypeSymbol>();
        }

        var typeArgs = new List<ITypeSymbol>();
        foreach (var arg in attribute.ConstructorArguments)
        {
            try
            {
                if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
                {
                    typeArgs.Add(typeSymbol);
                }
                else if (arg.Kind == TypedConstantKind.Array)
                {
                    // Handle array of types
                    foreach (var arrayElement in arg.Values)
                    {
                        if (arrayElement.Kind == TypedConstantKind.Type && arrayElement.Value is ITypeSymbol arrayTypeSymbol)
                        {
                            typeArgs.Add(arrayTypeSymbol);
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // If it's an array but we tried to access Value, skip it
                continue;
            }
        }

        return typeArgs.ToArray();
    }

    private void GenerateBaseTypes(IncrementalGeneratorPostInitializationContext context)
    {
        // All base types now exist in TUnit.Core - no need to generate any
    }

    private void GenerateTestClassSource(SourceProductionContext context, TestClassGroup testClassGroup)
    {
        try
        {
            // Add null check
            if (testClassGroup?.TypeSymbol == null || testClassGroup.TestMethods == null)
            {
                return;
            }
            var writer = new CodeWriter();

            // Generate file header
            GenerateFileHeader(writer);

            // Create generator instances
            // HookGenerator removed - hooks are now handled by UnifiedHookMetadataGenerator
            var dataSourceGenerator = new DataSourceGenerator();
            var genericTypeResolver = new GenericTypeResolver();
            var metadataGenerator = new MetadataGenerator(dataSourceGenerator);

            // Generate unique names based on the test class
            var fullTypeName = testClassGroup.TypeSymbol.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes))
                .Replace('.', '_')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace(',', '_')
                .Replace(' ', '_')
                .Replace('[', '_')
                .Replace(']', '_');
            
            var sourceClassName = $"{fullTypeName}_TestSource";
            var moduleInitializerClassName = $"{fullTypeName}_ModuleInitializer";

            // Generate the test source implementation
            writer.AppendLine($"internal sealed class {sourceClassName} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("private static readonly List<TestMetadata> _allTests = new();");
            writer.AppendLine();
            writer.AppendLine("public IEnumerable<TestMetadata> GetTests() => _allTests;");
            writer.AppendLine();

            // Static constructor to initialize tests
            writer.AppendLine($"static {sourceClassName}()");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("RegisterAllTests();");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("catch (Exception ex)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"Console.Error.WriteLine($\"Failed to initialize test source for {testClassGroup.TypeSymbol.Name}: {{ex}}\");");
            writer.AppendLine("throw;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();

            // Generate registration methods
            GenerateRegisterAllTests(writer, metadataGenerator, testClassGroup.TestMethods);

            // Generate generic test registry
            writer.AppendLine();
            writer.AppendLine("// Generic test registry");
            genericTypeResolver.GenerateGenericTestRegistry(writer);

            // Generate async data source wrapper methods
            writer.AppendLine();
            writer.AppendLine("// Async data source wrapper methods");
            dataSourceGenerator.GenerateAsyncDataSourceWrappers(writer, testClassGroup.TestMethods);

            // Generate helper methods that may be needed
            writer.AppendLine();
            writer.AppendLine("// Helper methods");
            GenerateHelperMethods(writer, dataSourceGenerator, testClassGroup.TestMethods);

            // Factory methods and delegate implementations are now embedded in TestMetadata

            // Hook implementations are now handled by UnifiedHookMetadataGenerator

            writer.Unindent();
            writer.AppendLine("}");

            writer.AppendLine();

            // Generate module initializer class
            writer.AppendLine($"internal static class {moduleInitializerClassName}");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
            writer.AppendLine("public static void Initialize()");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"global::TUnit.Core.SourceRegistrar.Register(new {sourceClassName}());");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");

            // Add source with unique filename
            var source = writer.ToString();
            var fileName = $"{fullTypeName}.TestSource.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
        catch (InvalidOperationException ioex) when (ioex.Message.Contains("TypedConstant is an array"))
        {
            // This is likely happening in one of the sub-generators
            var className = testClassGroup?.TypeSymbol?.Name ?? "Unknown";
            var errorMessage = $"TypedConstant array error in {className}: {ioex.Message}\n\nStack Trace:\n{ioex.StackTrace}";

            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TU0002",
                    "TypedConstant Array Error",
                    "Failed due to TypedConstant array access: {0}",
                    "TUnit.Generation",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                Location.None,
                errorMessage);

            context.ReportDiagnostic(diagnostic);
        }
        catch (Exception ex)
        {
            // Report diagnostic with full stack trace for debugging
            var className = testClassGroup?.TypeSymbol?.Name ?? "Unknown";
            var errorMessage = $"Failed to generate test source for {className}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TU0001",
                    "Test Generation Failed",
                    "Failed to generate test metadata: {0}",
                    "TUnit.Generation",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                errorMessage);

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
        writer.AppendLine("using System.Runtime;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine("using global::TUnit.Core.Services;");
        writer.AppendLine("using global::TUnit.Core.Interfaces.SourceGenerator;");
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
        writer.AppendLine("// Tests are registered through ITestSource in the static constructor");
        writer.AppendLine();

        // Create efficient test registry with proper equality comparers
        writer.AppendLine("// Register with the discovery service using efficient registry");
        writer.AppendLine("// Note: Registration will be handled by the test engine at runtime");
        writer.AppendLine();

        // Register generic test registry if available
        writer.AppendLine("// Register generic test combinations if any were discovered");
        writer.AppendLine("RegisterGenericTestCombinations();");
        writer.AppendLine();

        // Tests are now registered individually in RegisterAllTests() for better error isolation
        writer.AppendLine("// Tests are now registered individually in RegisterAllTests() for better error isolation");

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
    
    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.TypeArguments.Any(ContainsTypeParameter))
            {
                return true;
            }
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        return false;
    }
}

/// <summary>
/// Metadata for a test method
/// </summary>
public class TestMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public MethodDeclarationSyntax? MethodSyntax { get; init; }
    public ITypeSymbol[]? GenericTypeArguments { get; init; }
}

/// <summary>
/// Groups test methods by their containing class
/// </summary>
public class TestClassGroup
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required List<TestMethodMetadata> TestMethods { get; init; }
}
