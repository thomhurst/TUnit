using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static string GenerateClassInformation(GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedClassInformation<{{namedTypeSymbol.GloballyQualified()}}>
                 {    
                      Assembly = {{GenerateAssemblyInformation(context, namedTypeSymbol.ContainingAssembly)}},
                      Name = "{{namedTypeSymbol.Name}}",
                      Namespace = "{{namedTypeSymbol.ContainingNamespace.ToDisplayString()}}",
                      Attributes = 
                      [
                          {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, namedTypeSymbol.GetAttributes()))}}
                      ],  
                      Parameters = [{{string.Join(", \r\n", namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters.Select(p => GenerateParameterInformation(context, p, ArgumentsType.ClassConstructor)) ?? [])}}],
                      Properties = [{{string.Join(", \r\n", namedTypeSymbol.GetMembers().OfType<IPropertySymbol>().Select(p => GeneratePropertyInformation(context, p)))}}],
                 }
                 """;
    }

    private static string GenerateAssemblyInformation(GeneratorAttributeSyntaxContext context, IAssemblySymbol assembly)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedAssemblyInformation
                 {
                      Name = "{{assembly.Name}}",
                      Attributes = 
                      [
                          {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, assembly.GetAttributes()))}}
                      ],  
                 }
                 """;
    }

    public static string GenerateMethodInformation(GeneratorAttributeSyntaxContext context, IMethodSymbol methodSymbol)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedMethodInformation<{{methodSymbol.ContainingType.GloballyQualified()}}>
                 {
                      Name = "{{methodSymbol.Name}}",
                      GenericTypeCount = {{methodSymbol.TypeParameters.Length}},
                      ReturnType = typeof({{methodSymbol.ReturnType.GloballyQualified()}}),
                      Attributes = 
                      [
                          {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, methodSymbol.GetAttributes()))}}
                      ],  
                      Parameters = [{{string.Join(", \r\n", methodSymbol.Parameters.Select(p => GenerateParameterInformation(context, p, ArgumentsType.Method)))}}],
                      Class = {{GenerateClassInformation(context, methodSymbol.ContainingType)}},
                 }
                 """;
    }
    
    public static string GenerateMembers(GeneratorAttributeSyntaxContext context, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if (property is not null)
        {
            return $"[{GeneratePropertyInformation(context, property)}]";
        }

        return $"[{string.Join(", \r\n", parameters.Select(p => GenerateParameterInformation(context, p, argumentsType)))}]";
    }

    public static string GeneratePropertyInformation(GeneratorAttributeSyntaxContext context, IPropertySymbol property)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedPropertyInformation<{{property.Type.GloballyQualified()}}>
                     {
                         Name = "{{property.Name}}",
                         IsStatic = {{property.IsStatic.ToString().ToLower()}},
                         Attributes = 
                         [
                             {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, property.GetAttributes()))}}
                         ]
                     }
                 """;
    }
    
    public static string GenerateParameterInformation(GeneratorAttributeSyntaxContext context, IParameterSymbol parameter, ArgumentsType argumentsType)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedParameterInformation<{{parameter.Type.GloballyQualified()}}>
                     {
                         Name = "{{parameter.Name}}",
                         Attributes = 
                         [
                             {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, parameter.GetAttributes()))}}
                         ]
                     }
                 """;
    }
}