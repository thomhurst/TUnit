using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Builders;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Models;

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
            TestAttribute = context.Attributes[0],
            Context = context
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
            // Create context
            var generationContext = TestMetadataGenerationContext.Create(testInfo);
            
            using var writer = new CodeWriter();

            // Write file header
            WriteFileHeader(writer);
            
            // Write namespace and class declaration
            writer.AppendLine("namespace TUnit.Generated;");
            writer.AppendLine();
            using (writer.BeginBlock($"internal static class TestMetadataRegistry_{generationContext.SafeClassName}_{generationContext.SafeMethodName}_{generationContext.Guid}"))
            {
                writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
                using (writer.BeginBlock("public static void Initialize()"))
                {
                    using (writer.BeginBlock("try"))
                    {
                        // Select appropriate builder
                        ITestDefinitionBuilder builder = SelectBuilder(generationContext);
                        
                        // Generate test definitions
                        builder.BuildTestDefinitions(writer, generationContext);
                    }
                    
                    using (writer.BeginBlock("catch (System.Exception ex)"))
                    {
                        writer.AppendLine("// Runtime initialization failed - generate minimal metadata that will report the error");
                        writer.AppendLine("// Note: We can't call external methods here as this is in the module initializer");
                        writer.AppendLine("// So we need to generate the failure metadata inline");
                        GenerateInlineFailureMetadata(writer, generationContext);
                    }
                }
            }

            // Add the generated code to the compilation
            context.AddSource($"{generationContext.SafeClassName}_{generationContext.SafeMethodName}_{generationContext.Guid}.g.cs", writer.ToString());
        }
        catch (Exception ex)
        {
            // Report diagnostic
            ReportGenerationError(context, testInfo, ex);
            
            // Generate a failure test metadata
            GenerateFailureTestForSourceGenerationError(context, testInfo, ex);
        }
    }
    
    private static void WriteFileHeader(CodeWriter writer)
    {
        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Reflection;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine("using global::TUnit.Core.SourceGenerator;");
        writer.AppendLine();
    }
    
    private static ITestDefinitionBuilder SelectBuilder(TestMetadataGenerationContext context)
    {
        if (context.CanUseStaticDefinition)
        {
            return new StaticTestDefinitionBuilder();
        }
        else
        {
            return new DynamicTestMetadataBuilder();
        }
    }
    
    private static void GenerateInlineFailureMetadata(CodeWriter writer, TestMetadataGenerationContext context)
    {
        writer.AppendLine("var errorMessage = \"Runtime initialization failed: \" + ex.GetType().Name + \": \" + ex.Message;");
        writer.AppendLine();
        
        // Generate minimal failure metadata inline since we're in module initializer
        if (context.CanUseStaticDefinition)
        {
            writer.AppendLine("var testDescriptors = new System.Collections.Generic.List<ITestDescriptor>();");
            writer.AppendLine();
            
            using (writer.BeginObjectInitializer("var failureDef = new StaticTestDefinition", ";"))
            {
                writer.AppendLine($"TestId = \"{context.ClassName}.{context.MethodName}_RuntimeFailure_{{{{TestIndex}}}}\",");
                writer.AppendLine($"DisplayName = \"{context.MethodName} [RUNTIME INITIALIZATION FAILED]\",");
                writer.AppendLine($"TestFilePath = @\"{context.TestInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
                writer.AppendLine($"TestLineNumber = {context.TestInfo.LineNumber},");
                writer.AppendLine($"IsAsync = true,");
                writer.AppendLine($"IsSkipped = false,");
                writer.AppendLine($"SkipReason = null,");
                writer.AppendLine($"Timeout = null,");
                writer.AppendLine($"RepeatCount = 1,");
                writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
                writer.AppendLine($"TestMethodMetadata = {GenerateFailureMethodMetadataInline(context)},");
                writer.AppendLine($"ClassFactory = args => throw new InvalidOperationException(errorMessage),");
                writer.AppendLine($"MethodInvoker = async (instance, args, cancellationToken) => {{ await Task.CompletedTask; throw new InvalidOperationException(errorMessage); }},");
                writer.AppendLine($"PropertyValuesProvider = () => new[] {{ new System.Collections.Generic.Dictionary<string, object?>() }},");
                writer.AppendLine($"ClassDataProvider = new TUnit.Core.EmptyDataProvider(),");
                writer.AppendLine($"MethodDataProvider = new TUnit.Core.EmptyDataProvider()");
            }
            
            writer.AppendLine("testDescriptors.Add(failureDef);");
            writer.AppendLine("TestSourceRegistrar.RegisterTests(testDescriptors);");
        }
        else
        {
            writer.AppendLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
            writer.AppendLine();
            
            using (writer.BeginObjectInitializer("var failureMetadata = new DynamicTestMetadata", ";"))
            {
                writer.AppendLine($"TestIdTemplate = \"{context.ClassName}.{context.MethodName}_RuntimeFailure_{{{{TestIndex}}}}\",");
                writer.AppendLine($"TestClassTypeReference = TUnit.Core.TypeReference.CreateConcrete(\"{context.ClassName}\"),");
                writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
                writer.AppendLine($"TestClassFactory = args => throw new InvalidOperationException(errorMessage),");
                
                using (writer.BeginObjectInitializer("MethodMetadata = new global::TUnit.Core.MethodMetadata", ","))
                {
                    writer.AppendLine($"Type = typeof({context.ClassName}),");
                    writer.AppendLine($"TypeReference = TUnit.Core.TypeReference.CreateConcrete(\"{context.ClassName}\"),");
                    writer.AppendLine($"Name = \"{context.MethodName}_RuntimeFailure\",");
                    writer.AppendLine($"GenericTypeCount = 0,");
                    writer.AppendLine($"ReturnType = typeof(global::System.Threading.Tasks.Task),");
                    writer.AppendLine($"ReturnTypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"System.Threading.Tasks.Task, System.Private.CoreLib\"),");
                    writer.AppendLine($"Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }},");
                    writer.AppendLine($"Parameters = new global::TUnit.Core.ParameterMetadata[] {{ }},");
                    
                    using (writer.BeginObjectInitializer("Class = new global::TUnit.Core.ClassMetadata", ","))
                    {
                        writer.AppendLine($"Name = \"{context.TestInfo.TypeSymbol.Name}\",");
                        writer.AppendLine($"Type = typeof({context.ClassName}),");
                        writer.AppendLine($"TypeReference = TUnit.Core.TypeReference.CreateConcrete(\"{context.ClassName}\"),");
                        writer.AppendLine($"Namespace = \"{context.TestInfo.TypeSymbol.ContainingNamespace.ToDisplayString()}\",");
                        writer.AppendLine($"Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }},");
                        writer.AppendLine($"Properties = new global::TUnit.Core.PropertyMetadata[] {{ }},");
                        writer.AppendLine($"Parameters = new global::TUnit.Core.ParameterMetadata[] {{ }},");
                        writer.AppendLine($"Parent = null,");
                        
                        writer.AppendLine($"Assembly = new global::TUnit.Core.AssemblyMetadata");
                        writer.AppendLine("{");
                        writer.AppendLine($"    Name = \"{context.TestInfo.TypeSymbol.ContainingAssembly.Name}\",");
                        writer.AppendLine($"    Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }}");
                        writer.AppendLine("}");
                    }
                    
                    writer.AppendLine($"ReflectionInformation = null");
                }
                
                writer.AppendLine($"TestFilePath = @\"{context.TestInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
                writer.AppendLine($"TestLineNumber = {context.TestInfo.LineNumber},");
                writer.AppendLine($"ClassDataSources = System.Array.Empty<global::TUnit.Core.IDataSourceProvider>(),");
                writer.AppendLine($"MethodDataSources = System.Array.Empty<global::TUnit.Core.IDataSourceProvider>(),");
                writer.AppendLine($"PropertyDataSources = new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.IDataSourceProvider>(),");
                writer.AppendLine($"DisplayNameTemplate = \"{context.MethodName} [RUNTIME INITIALIZATION FAILED]\",");
                writer.AppendLine($"RepeatCount = 1,");
                writer.AppendLine($"IsAsync = true,");
                writer.AppendLine($"IsSkipped = false,");
                writer.AppendLine($"SkipReason = null,");
                writer.AppendLine($"Timeout = null");
            }
            
            writer.AppendLine("testMetadata.Add(failureMetadata);");
            writer.AppendLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
        }
    }
    
    private static void ReportGenerationError(SourceProductionContext context, TestMethodInfo testInfo, Exception ex)
    {
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
    
    private static void GenerateFailureTestForSourceGenerationError(SourceProductionContext context, TestMethodInfo testInfo, Exception ex)
    {
        try
        {
            var generationContext = TestMetadataGenerationContext.Create(testInfo);
            using var writer = new CodeWriter();
            
            var errorMessage = $"Source generation failed: {ex.GetType().Name}: {ex.Message}";
            FailureTestBuilder.GenerateFailureTest(writer, generationContext, errorMessage);
            
            context.AddSource($"{generationContext.SafeClassName}_{generationContext.SafeMethodName}_{generationContext.Guid}_Failure.g.cs", writer.ToString());
        }
        catch
        {
            // If we can't even generate the failure test, just swallow the exception
        }
    }

    private static string GenerateFailureMethodMetadataInline(TestMetadataGenerationContext context)
    {
        var assemblyQualifiedName = $"{context.ClassName}, {context.TestInfo.TypeSymbol.ContainingAssembly.Name}";
        
        // Generate inline metadata using the same pattern as FailureTestBuilder
        using var writer = new CodeWriter("", includeHeader: false);
        
        using (writer.BeginObjectInitializer("new global::TUnit.Core.MethodMetadata", ""))
        {
            writer.AppendLine($"Type = typeof({context.ClassName}),");
            writer.AppendLine($"TypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"{assemblyQualifiedName}\"),");
            writer.AppendLine($"Name = \"{context.MethodName}_RuntimeFailure\",");
            writer.AppendLine($"GenericTypeCount = 0,");
            writer.AppendLine($"ReturnType = typeof(global::System.Threading.Tasks.Task),");
            writer.AppendLine($"ReturnTypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"System.Threading.Tasks.Task, System.Private.CoreLib\"),");
            writer.AppendLine($"Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }},");
            writer.AppendLine($"Parameters = new global::TUnit.Core.ParameterMetadata[] {{ }},");
            
            using (writer.BeginObjectInitializer("Class = new global::TUnit.Core.ClassMetadata", ","))
            {
                writer.AppendLine($"Name = \"{context.TestInfo.TypeSymbol.Name}\",");
                writer.AppendLine($"Type = typeof({context.ClassName}),");
                writer.AppendLine($"TypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"{assemblyQualifiedName}\"),");
                writer.AppendLine($"Namespace = \"{context.TestInfo.TypeSymbol.ContainingNamespace.ToDisplayString()}\",");
                writer.AppendLine($"Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }},");
                writer.AppendLine($"Properties = new global::TUnit.Core.PropertyMetadata[] {{ }},");
                writer.AppendLine($"Parameters = new global::TUnit.Core.ParameterMetadata[] {{ }},");
                writer.AppendLine($"Parent = null,");
                
                writer.AppendLine($"Assembly = new global::TUnit.Core.AssemblyMetadata");
                writer.AppendLine("{");
                writer.AppendLine($"    Name = \"{context.TestInfo.TypeSymbol.ContainingAssembly.Name}\",");
                writer.AppendLine($"    Attributes = new global::TUnit.Core.AttributeMetadata[] {{ }}");
                writer.AppendLine("}");
            }
            
            writer.AppendLine($"ReflectionInformation = null");
        }
        
        return writer.ToString().Trim();
    }
}