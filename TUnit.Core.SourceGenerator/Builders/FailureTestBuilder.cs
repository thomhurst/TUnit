using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Builders;

/// <summary>
/// Builds test definitions for runtime failures
/// </summary>
public class FailureTestBuilder
{
    /// <summary>
    /// Generates a failure test metadata for a test that failed during initialization
    /// </summary>
    public static void GenerateFailureTest(CodeWriter writer, TestMetadataGenerationContext context, string errorMessage)
    {
        // Write header
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
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        writer.AppendLine($"public static class TestInit_{context.SafeClassName}_{context.SafeMethodName}_{context.Guid}");
        writer.AppendLine("{");
        writer.AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("    public static void Initialize()");
        writer.AppendLine("    {");
        writer.AppendLine($"        var errorMessage = @\"{errorMessage.Replace("\\", "\\\\").Replace("\"", "\\\"")}\";");
        writer.AppendLine();

        if (context.CanUseStaticDefinition)
        {
            GenerateStaticFailureDefinition(writer, context);
        }
        else
        {
            GenerateDynamicFailureMetadata(writer, context);
        }

        writer.AppendLine("    }");
        writer.AppendLine("}");
    }

    private static void GenerateStaticFailureDefinition(CodeWriter writer, TestMetadataGenerationContext context)
    {
        writer.AppendLine("        var testDescriptors = new System.Collections.Generic.List<ITestDescriptor>();");
        writer.AppendLine();

        using (writer.BeginObjectInitializer("        var failureDef = new StaticTestDefinition"))
        {
            writer.AppendLine($"TestId = \"{context.ClassName}.{context.MethodName}_RuntimeFailure_{{{{TestIndex}}}}\",");
            writer.AppendLine($"DisplayName = \"{context.MethodName} [RUNTIME INITIALIZATION FAILED]\",");
            writer.AppendLine($"TestFilePath = @\"{context.TestInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {context.TestInfo.LineNumber},");
            writer.AppendLine("IsAsync = true,");
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
            writer.AppendLine("Timeout = null,");
            writer.AppendLine("RepeatCount = 1,");
            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
            writer.AppendLine($"TestMethodMetadata = {GenerateFailureMethodMetadata(context)},");
            writer.AppendLine($"ClassFactory = args => new {context.ClassName}(),");
            writer.AppendLine("MethodInvoker = async (instance, args, cancellationToken) => throw new InvalidOperationException(errorMessage),");
            writer.AppendLine("PropertyValuesProvider = () => new[] { new System.Collections.Generic.Dictionary<string, object?>() },");
            writer.AppendLine("ClassDataProvider = new TUnit.Core.EmptyDataProvider(),");
            writer.AppendLine("MethodDataProvider = new TUnit.Core.EmptyDataProvider()");
        }

        writer.AppendLine("        testDescriptors.Add(failureDef);");
        writer.AppendLine("        TestSourceRegistrar.RegisterTests(testDescriptors);");
    }

    private static void GenerateDynamicFailureMetadata(CodeWriter writer, TestMetadataGenerationContext context)
    {
        writer.AppendLine("        var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
        writer.AppendLine();

        using (writer.BeginObjectInitializer("        var failureMetadata = new DynamicTestMetadata"))
        {
            writer.AppendLine($"TestIdTemplate = \"{context.ClassName}.{context.MethodName}_RuntimeFailure_{{{{TestIndex}}}}\",");
            writer.AppendLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(context.TestInfo.TypeSymbol)},");
            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
            writer.AppendLine("TestClassFactory = args => throw new InvalidOperationException(errorMessage),");

            using (writer.BeginObjectInitializer("MethodMetadata = new global::TUnit.Core.MethodMetadata", ","))
            {
                writer.AppendLine($"Type = typeof({context.ClassName}),");
                writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(context.TestInfo.TypeSymbol)},");
                writer.AppendLine($"Name = \"{context.MethodName}_RuntimeFailure\",");
                writer.AppendLine("GenericTypeCount = 0,");
                writer.AppendLine("ReturnType = typeof(global::System.Threading.Tasks.Task),");
                writer.AppendLine("ReturnTypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"System.Threading.Tasks.Task, System.Private.CoreLib\"),");
                writer.AppendLine("Attributes = new global::TUnit.Core.AttributeMetadata[] { },");
                writer.AppendLine("Parameters = new global::TUnit.Core.ParameterMetadata[] { },");

                using (writer.BeginObjectInitializer("Class = new global::TUnit.Core.ClassMetadata", ","))
                {
                    writer.AppendLine($"Name = \"{context.TestInfo.TypeSymbol.Name}\",");
                    writer.AppendLine($"Type = typeof({context.ClassName}),");
                    writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(context.TestInfo.TypeSymbol)},");
                    writer.AppendLine($"Namespace = \"{context.TestInfo.TypeSymbol.ContainingNamespace.ToDisplayString()}\",");
                    writer.AppendLine("Attributes = new global::TUnit.Core.AttributeMetadata[] { },");
                    writer.AppendLine("Properties = new global::TUnit.Core.PropertyMetadata[] { },");
                    writer.AppendLine("Parameters = new global::TUnit.Core.ParameterMetadata[] { },");
                    writer.AppendLine("Parent = null,");

                    writer.AppendLine("Assembly = new global::TUnit.Core.AssemblyMetadata");
                    writer.AppendLine("{");
                    writer.AppendLine($"    Name = \"{context.TestInfo.TypeSymbol.ContainingAssembly.Name}\",");
                    writer.AppendLine("    Attributes = new global::TUnit.Core.AttributeMetadata[] { }");
                    writer.AppendLine("}");
                }

                writer.AppendLine("ReflectionInformation = null");
            }

            writer.AppendLine($"TestFilePath = @\"{context.TestInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {context.TestInfo.LineNumber},");
            writer.AppendLine("ClassDataSources = System.Array.Empty<global::TUnit.Core.TestDataSource>(),");
            writer.AppendLine("MethodDataSources = System.Array.Empty<global::TUnit.Core.TestDataSource>(),");
            writer.AppendLine("PropertyDataSources = new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.TestDataSource>(),");
            writer.AppendLine($"DisplayNameTemplate = \"{context.MethodName} [RUNTIME INITIALIZATION FAILED]\",");
            writer.AppendLine("RepeatCount = 1,");
            writer.AppendLine("IsAsync = true,");
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
            writer.AppendLine("Timeout = null");
        }

        writer.AppendLine("        testMetadata.Add(failureMetadata);");
        writer.AppendLine("        TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
    }

    private static string GenerateFailureMethodMetadata(TestMetadataGenerationContext context)
    {
        using var writer = new CodeWriter("", includeHeader: false);

        using (writer.BeginObjectInitializer("new global::TUnit.Core.MethodMetadata", ""))
        {
            writer.AppendLine($"Type = typeof({context.ClassName}),");
            writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(context.TestInfo.TypeSymbol)},");
            writer.AppendLine($"Name = \"{context.MethodName}_RuntimeFailure\",");
            writer.AppendLine("GenericTypeCount = 0,");
            writer.AppendLine("ReturnType = typeof(global::System.Threading.Tasks.Task),");
            writer.AppendLine("ReturnTypeReference = global::TUnit.Core.TypeReference.CreateConcrete(\"System.Threading.Tasks.Task, System.Private.CoreLib\"),");
            writer.AppendLine("Attributes = new global::TUnit.Core.AttributeMetadata[] { },");
            writer.AppendLine("Parameters = new global::TUnit.Core.ParameterMetadata[] { },");

            using (writer.BeginObjectInitializer("Class = new global::TUnit.Core.ClassMetadata", ","))
            {
                writer.AppendLine($"Name = \"{context.TestInfo.TypeSymbol.Name}\",");
                writer.AppendLine($"Type = typeof({context.ClassName}),");
                writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(context.TestInfo.TypeSymbol)},");
                writer.AppendLine($"Namespace = \"{context.TestInfo.TypeSymbol.ContainingNamespace.ToDisplayString()}\",");
                writer.AppendLine("Attributes = new global::TUnit.Core.AttributeMetadata[] { },");
                writer.AppendLine("Properties = new global::TUnit.Core.PropertyMetadata[] { },");
                writer.AppendLine("Parameters = new global::TUnit.Core.ParameterMetadata[] { },");
                writer.AppendLine("Parent = null,");

                writer.AppendLine("Assembly = new global::TUnit.Core.AssemblyMetadata");
                writer.AppendLine("{");
                writer.AppendLine($"    Name = \"{context.TestInfo.TypeSymbol.ContainingAssembly.Name}\",");
                writer.AppendLine("    Attributes = new global::TUnit.Core.AttributeMetadata[] { }");
                writer.AppendLine("}");
            }

            writer.AppendLine("ReflectionInformation = null");
        }

        return writer.ToString().Trim();
    }
}
