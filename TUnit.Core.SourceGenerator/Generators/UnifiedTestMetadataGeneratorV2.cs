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

        // Find all test methods using the more performant ForAttributeWithMetadataName
        var testMethodsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null);

        // Generate one source file per test method
        context.RegisterSourceOutput(testMethodsProvider, static (context, testMethod) => GenerateTestMethodSource(context, testMethod));

        // Handle generic tests with explicit type arguments
        var genericTestsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.GenerateGenericTestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, _) => GetGenericTestMetadata(ctx))
            .Where(static m => m is not null)
            .SelectMany(static (m, _) => m!);

        // Generate one file per generic test instantiation
        context.RegisterSourceOutput(genericTestsProvider, static (context, testMethod) => GenerateTestMethodSource(context, testMethod));

        // Handle inferred generic tests (using CreateSyntaxProvider for complex logic)
        var inferredGenericTests = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsGenericTestMethodOrClass(node),
                transform: static (ctx, _) => GetInferredGenericTestMetadata(ctx))
            .Where(static m => m is not null)
            .SelectMany(static (m, _) => m!);

        context.RegisterSourceOutput(inferredGenericTests, static (context, testMethod) => GenerateTestMethodSource(context, testMethod));
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


    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

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

    private static IEnumerable<TestMethodMetadata>? GetGenericTestMetadata(GeneratorAttributeSyntaxContext context)
    {
        var results = new List<TestMethodMetadata>();

        if (context.TargetNode is ClassDeclarationSyntax classDecl)
        {
            var classSymbol = context.TargetSymbol as INamedTypeSymbol;
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

    private static void GenerateTestMethodSource(SourceProductionContext context, TestMethodMetadata? testMethod)
    {
        try
        {
            // Add null check
            if (testMethod?.MethodSymbol == null || testMethod.TypeSymbol == null)
            {
                return;
            }
            
            var writer = new CodeWriter();

            // Generate file header
            GenerateFileHeader(writer);

            // Create generator instances
            var dataSourceGenerator = new DataSourceGenerator();
            var metadataGenerator = new MetadataGenerator(dataSourceGenerator);

            // Generate unique names based on the test method
            var typeFullName = testMethod.TypeSymbol.ToDisplayString(new SymbolDisplayFormat(
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
            
            var methodName = testMethod.MethodSymbol.Name;
            var sourceClassName = $"{typeFullName}_{methodName}_TestSource";
            var moduleInitializerClassName = $"{typeFullName}_{methodName}_ModuleInitializer";

            // Generate the test source implementation
            writer.AppendLine($"internal sealed class {sourceClassName} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("private static readonly TestMetadata _testMetadata;");
            writer.AppendLine();
            writer.AppendLine("public IEnumerable<TestMetadata> GetTests() { yield return _testMetadata; }");
            writer.AppendLine();

            // Static constructor to initialize the test
            writer.AppendLine($"static {sourceClassName}()");
            writer.AppendLine("{");
            writer.Indent();
            
            // Generate the metadata for this single test method
            metadataGenerator.GenerateSingleTestMetadata(writer, testMethod);
            
            writer.Unindent();
            writer.AppendLine("}");

            // Generate any async data source wrapper methods needed for this test
            var testMethods = new List<TestMethodMetadata> { testMethod };
            if (HasAsyncDataSources(testMethod))
            {
                writer.AppendLine();
                writer.AppendLine("// Async data source wrapper methods");
                dataSourceGenerator.GenerateAsyncDataSourceWrappers(writer, testMethods);
                
                // Also generate the ConvertToSync helper if needed
                writer.AppendLine();
                GenerateConvertToSyncHelper(writer);
            }

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
            var fileName = $"{typeFullName}.{methodName}.TestSource.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            // Report diagnostic with full stack trace for debugging
            var methodName = testMethod?.MethodSymbol?.Name ?? "Unknown";
            var className = testMethod?.TypeSymbol?.Name ?? "Unknown";
            var errorMessage = $"Failed to generate test source for {className}.{methodName}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

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

    private static bool HasAsyncDataSources(TestMethodMetadata testMethod)
    {
        var hasMethodDataSource = testMethod.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
        
        var hasPropertyDataSource = testMethod.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "DataSourceForAttribute"));
            
        return hasMethodDataSource || hasPropertyDataSource;
    }

    private static void GenerateFileHeader(CodeWriter writer)
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




    private static void GenerateConvertToSyncHelper(CodeWriter writer)
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

