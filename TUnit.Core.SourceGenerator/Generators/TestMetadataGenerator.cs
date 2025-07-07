using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Main orchestrator for test metadata generation following Single Responsibility Principle
/// </summary>
[Generator]
public sealed class TestMetadataGenerator : IIncrementalGenerator
{
    private const string GeneratedNamespace = "TUnit.Generated";

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

        var containingType = methodSymbol?.ContainingType;

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
        if (containingType is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            return null;
        }

        // Also check if any type arguments are type parameters (e.g., T)
        if (containingType is { IsGenericType: true })
        {
            if (containingType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter))
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

            if (currentType is { IsGenericType: true } baseNamedType)
            {
                if (baseNamedType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter))
                {
                    return null;
                }
            }

            currentType = currentType.BaseType;
        }

        // Skip generic methods without explicit instantiation
        if (methodSymbol is { IsGenericMethod: true })
        {
            return null;
        }

        // Also skip if method has parameters with unresolved type parameters
        if (methodSymbol != null &&
            methodSymbol.Parameters.Any(p => ContainsTypeParameter(p.Type)))
        {
            return null;
        }

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol ?? throw new InvalidOperationException("Symbol is not a method"),
            TypeSymbol = containingType,
            MethodSyntax = methodSyntax
        };
    }

    private static IEnumerable<TestMethodMetadata>? GetGenericTestMetadata(GeneratorAttributeSyntaxContext context)
    {
        var results = new List<TestMethodMetadata>();

        if (context.TargetNode is ClassDeclarationSyntax)
        {
            // Get all generic test methods in the class
            if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            // Skip abstract classes (cannot be instantiated)
            if (classSymbol.IsAbstract)
            {
                return null;
            }

            var genericMethods = classSymbol.GetMembers()
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
                        TypeSymbol = classSymbol,
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
            if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol || classSymbol.IsAbstract)
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

            var containingType = methodSymbol?.ContainingType;
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
                if (arg is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
                {
                    typeArgs.Add(typeSymbol);
                }
                else if (arg.Kind == TypedConstantKind.Array)
                {
                    // Handle array of types
                    foreach (var arrayElement in arg.Values)
                    {
                        if (arrayElement is { Kind: TypedConstantKind.Type, Value: ITypeSymbol arrayTypeSymbol })
                        {
                            typeArgs.Add(arrayTypeSymbol);
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // If it's an array but we tried to access Value, skip it
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
            var typedMetadataGenerator = new TypedMetadataGenerator(dataSourceGenerator);

            // Check if test has [Arguments] attributes
            var hasArgumentsAttributes = testMethod.MethodSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "ArgumentsAttribute");

            if (hasArgumentsAttributes)
            {
                // Use new typed metadata generator for compile-time expansion
                GenerateExpandedTestMethodSource(context, testMethod, writer, typedMetadataGenerator);
            }
            else
            {
                // Use existing approach for tests without [Arguments]
                GenerateStandardTestMethodSource(context, testMethod, writer, dataSourceGenerator);
            }
        }
        catch (Exception ex)
        {
            // Report diagnostic with full stack trace for debugging
            var methodName = testMethod?.MethodSymbol?.Name ?? "Unknown";
            var className = testMethod?.TypeSymbol?.Name ?? "Unknown";
            
            // Main error diagnostic
            var diagnostic1 = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TU0001",
                    "Test Generation Failed",
                    "Failed to generate test metadata for {0}.{1}: {2}",
                    "TUnit.Generation",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                className, methodName, ex.Message);

            context.ReportDiagnostic(diagnostic1);

            // Stack trace as separate diagnostic(s)
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                var stackLines = ex.StackTrace.Split('\n');
                for (int i = 0; i < Math.Min(stackLines.Length, 10); i++) // Limit to first 10 lines
                {
                    var diagnostic2 = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "TU0002",
                            "Stack Trace",
                            "  {0}",
                            "TUnit.Generation", 
                            DiagnosticSeverity.Info,
                            isEnabledByDefault: true),
                        Location.None,
                        stackLines[i].Trim());
                    
                    context.ReportDiagnostic(diagnostic2);
                }
            }
        }
    }

    private static void GenerateExpandedTestMethodSource(SourceProductionContext context, TestMethodMetadata testMethod, 
        CodeWriter writer, TypedMetadataGenerator typedMetadataGenerator)
    {
        // Generate unique names based on the test method with GUID for uniqueness
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
        var guid = Guid.NewGuid().ToString("N");
        var sourceClassName = $"{typeFullName}_{methodName}_TestSource_{guid}";
        var moduleInitializerClassName = $"{typeFullName}_{methodName}_ModuleInitializer_{guid}";

        // Generate the test source implementation
        writer.AppendLine($"internal sealed class {sourceClassName} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("public async ValueTask<List<TestMetadata>> GetTestsAsync()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var _allTests = new List<TestMetadata>();");
        writer.AppendLine();

        // Generate expanded metadata for all [Arguments] variations
        typedMetadataGenerator.GenerateExpandedTestRegistrations(writer, new[] { testMethod });

        writer.AppendLine();
        writer.AppendLine("return _allTests;");
        writer.Unindent();
        writer.AppendLine("}");

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

        // Add source with unique filename including GUID
        var source = writer.ToString();
        var fileName = $"{typeFullName}.{methodName}.{guid}.TestSource.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private static void GenerateStandardTestMethodSource(SourceProductionContext context, TestMethodMetadata testMethod, 
        CodeWriter writer, DataSourceGenerator dataSourceGenerator)
    {
        var metadataGenerator = new MetadataGenerator(dataSourceGenerator);

        // Generate unique names based on the test method with GUID for uniqueness
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
        var guid = Guid.NewGuid().ToString("N");
        var sourceClassName = $"{typeFullName}_{methodName}_TestSource_{guid}";
        var moduleInitializerClassName = $"{typeFullName}_{methodName}_ModuleInitializer_{guid}";

        // Generate the test source implementation
        writer.AppendLine($"internal sealed class {sourceClassName} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        writer.AppendLine("{");
        writer.Indent();
        
        // Generate any async data source wrapper methods needed for this test
        var testMethods = new List<TestMethodMetadata> { testMethod };
        if (HasAsyncDataSources(testMethod))
        {
            writer.AppendLine("// Async data source wrapper methods");
            dataSourceGenerator.GenerateAsyncDataSourceWrappers(writer, testMethods);

            // Also generate the ConvertToSync helper if needed
            writer.AppendLine();
            GenerateConvertToSyncHelper(writer);
            writer.AppendLine();
        }
        
        writer.AppendLine("public async ValueTask<List<TestMetadata>> GetTestsAsync()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var _allTests = new List<TestMetadata>();");
        writer.AppendLine();

        // Generate the metadata for this single test method directly in the method
        metadataGenerator.GenerateTestRegistrations(writer, new[] { testMethod });

        writer.AppendLine();
        writer.AppendLine("return _allTests;");
        writer.Unindent();
        writer.AppendLine("}");

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

        // Add source with unique filename including GUID
        var source = writer.ToString();
        var fileName = $"{typeFullName}.{methodName}.{guid}.TestSource.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
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

