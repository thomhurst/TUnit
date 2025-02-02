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
                 new global::TUnit.Core.SourceGeneratedClassInformation
                 {    
                      Type = typeof({{namedTypeSymbol.GloballyQualified()}}),
                      Assembly = {{GenerateAssemblyInformation(context, namedTypeSymbol.ContainingAssembly)}},
                      Name = "{{namedTypeSymbol.Name}}",
                      Namespace = "{{namedTypeSymbol.ContainingNamespace.ToDisplayString()}}",
                      Attributes = 
                      [
                          {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, namedTypeSymbol.GetAttributes()))}}
                      ],  
                      Parameters = [{{string.Join(", \r\n", namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters.Select(p => GenerateParameterInformation(context, p, ArgumentsType.ClassConstructor, null)) ?? [])}}],
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

    public static string GenerateMethodInformation(GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedMethodInformation
                 {
                      Type = typeof({{namedTypeSymbol.GloballyQualified()}}),
                      Name = "{{methodSymbol.Name}}",
                      GenericTypeCount = {{methodSymbol.TypeParameters.Length}},
                      ReturnType = typeof({{methodSymbol.ReturnType.GloballyQualified()}}),
                      Attributes = 
                      [
                          {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, methodSymbol.GetAttributes()))}}
                      ],  
                      Parameters = [{{string.Join(", \r\n", methodSymbol.Parameters.Select(p => GenerateParameterInformation(context, p, ArgumentsType.Method, genericSubstitutions)))}}],
                      Class = {{GenerateClassInformation(context, methodSymbol.ReceiverType as INamedTypeSymbol ?? methodSymbol.ContainingType)}},
                 }
                 """;
    }
    
    public static string GenerateMembers(GeneratorAttributeSyntaxContext context, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if (property is not null)
        {
            return $"[{GeneratePropertyInformation(context, property)}]";
        }

        return $"[{string.Join(", \r\n", parameters.Select(p => GenerateParameterInformation(context, p, argumentsType, null)))}]";
    }

    public static string GeneratePropertyInformation(GeneratorAttributeSyntaxContext context, IPropertySymbol property)
    {
        return $$"""
                 new global::TUnit.Core.SourceGeneratedPropertyInformation
                     {
                         Type = typeof({{property.Type.GloballyQualified()}}),
                         Name = "{{property.Name}}",
                         IsStatic = {{property.IsStatic.ToString().ToLower()}},
                         Attributes = 
                         [
                             {{string.Join(", \r\n", AttributeWriter.WriteAttributes(context, property.GetAttributes()))}}
                         ]
                     }
                 """;
    }
    
    public static string GenerateParameterInformation(GeneratorAttributeSyntaxContext context,
        IParameterSymbol parameter, ArgumentsType argumentsType, IDictionary<string, string>? genericSubstitutions)
    {
        var type = parameter.Type.GloballyQualified();

        if (parameter.Type.IsGenericDefinition())
        {
            type = genericSubstitutions?.TryGetValue(type, out var substitution) == true 
                ? substitution
                // We can't find the generic type - Fall back to object
                : "object";
        }
        
        return $$"""
                 new global::TUnit.Core.SourceGeneratedParameterInformation<{{type}}>
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