using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class TestAttributeWriter
{
    public static void WriteTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ImmutableArray<AttributeData> attributeDatas, string targetElement, string? targetMemberName = null, ITypeSymbol? targetType = null)
    {
        if (attributeDatas.Length == 0)
        {
            sourceCodeWriter.Write("[],");
            return;
        }

        sourceCodeWriter.Write("[");
        for (var index = 0; index < attributeDatas.Length; index++)
        {
            var attributeData = attributeDatas[index];

            if (attributeData.ApplicationSyntaxReference is null && attributeData.AttributeClass?.ContainingAssembly?.Identity.Name != "mscorlib")
            {
                continue;
            }

            WriteTestAttribute(sourceCodeWriter, context, attributeData, targetElement, targetMemberName, targetType);

            if (index != attributeDatas.Length - 1)
            {
                sourceCodeWriter.Write(",");
            }

            sourceCodeWriter.WriteLine();
        }
        sourceCodeWriter.Write("],");
    }

    public static void WriteTestAttribute(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        AttributeData attributeData, string targetElement, string? targetMemberName = null, ITypeSymbol? targetType = null)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.AttributeMetadata");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write("{");
        sourceCodeWriter.WriteLine();

        // Write the Instance property - use existing AttributeWriter
        sourceCodeWriter.Write("Instance = ");
        AttributeWriter.WriteAttribute(sourceCodeWriter, context, attributeData);
        sourceCodeWriter.Write(",");
        sourceCodeWriter.WriteLine();

        // Write the TargetElement
        sourceCodeWriter.Write($"TargetElement = global::TUnit.Core.TestAttributeTarget.{targetElement},");
        sourceCodeWriter.WriteLine();

        // Write the TargetMemberName if provided
        if (!string.IsNullOrEmpty(targetMemberName))
        {
            sourceCodeWriter.Write($"TargetMemberName = \"{targetMemberName}\",");
            sourceCodeWriter.WriteLine();
        }

        // Write the TargetType if provided
        if (targetType != null)
        {
            sourceCodeWriter.Write($"TargetType = typeof({targetType.GloballyQualified()}),");
            sourceCodeWriter.WriteLine();
        }

        // Write ClassMetadata for the attribute type
        WriteAttributeClassMetadata(sourceCodeWriter, context, attributeData);

        // Write constructor arguments
        WriteConstructorArguments(sourceCodeWriter, attributeData);

        // Write named arguments
        WriteNamedArguments(sourceCodeWriter, attributeData);

        sourceCodeWriter.Write("}");
    }

    private static void WriteConstructorArguments(SourceCodeWriter sourceCodeWriter, AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length == 0)
        {
            return;
        }

        sourceCodeWriter.Write("ConstructorArguments = new object?[]");
        sourceCodeWriter.Write("{");

        for (var i = 0; i < attributeData.ConstructorArguments.Length; i++)
        {
            var arg = attributeData.ConstructorArguments[i];
            sourceCodeWriter.Write(TypedConstantParser.GetRawTypedConstantValue(arg));

            if (i < attributeData.ConstructorArguments.Length - 1)
            {
                sourceCodeWriter.Write(",");
            }
        }

        sourceCodeWriter.Write("}");
        sourceCodeWriter.Write(",");
        sourceCodeWriter.WriteLine();
    }

    private static void WriteNamedArguments(SourceCodeWriter sourceCodeWriter, AttributeData attributeData)
    {
        if (attributeData.NamedArguments.Length == 0)
        {
            return;
        }

        sourceCodeWriter.Write("NamedArguments = new global::System.Collections.Generic.Dictionary<string, object?>");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write("{");
        sourceCodeWriter.WriteLine();

        foreach (var namedArg in attributeData.NamedArguments)
        {
            sourceCodeWriter.Write($"{{ \"{namedArg.Key}\", {TypedConstantParser.GetRawTypedConstantValue(namedArg.Value)} }},");
            sourceCodeWriter.WriteLine();
        }

        sourceCodeWriter.Write("}");
            sourceCodeWriter.Write(",");
        sourceCodeWriter.WriteLine();
    }

    private static void WriteAttributeClassMetadata(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, AttributeData attributeData)
    {
        var attributeType = attributeData.AttributeClass;

        // Skip system attributes
        if (attributeType?.ContainingNamespace?.ToDisplayString()?.StartsWith("System") == true)
        {
            return;
        }

        // For source generated attributes, we can generate the ClassMetadata
        sourceCodeWriter.Write("ClassMetadata = ");
        SourceInformationWriter.GenerateClassInformation(sourceCodeWriter, context, attributeType!);
        sourceCodeWriter.Write(",");
        sourceCodeWriter.WriteLine();
    }

    // Helper methods for different contexts
    public static void WriteAssemblyTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IAssemblySymbol assembly)
    {
        var attributes = assembly.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Assembly", assembly.Name);
    }

    public static void WriteTypeTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        ITypeSymbol type)
    {
        var attributes = type.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Class", type.Name, type);
    }

    public static void WriteMethodTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IMethodSymbol method)
    {
        var attributes = method.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Method", method.Name, method.ContainingType);
    }

    public static void WritePropertyTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IPropertySymbol property)
    {
        var attributes = property.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Property", property.Name, property.ContainingType);
    }

    public static void WriteParameterTestAttributes(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context,
        IParameterSymbol parameter)
    {
        var attributes = parameter.GetAttributes();
        WriteTestAttributes(sourceCodeWriter, context, attributes, "Parameter", parameter.Name, parameter.ContainingType);
    }
}
