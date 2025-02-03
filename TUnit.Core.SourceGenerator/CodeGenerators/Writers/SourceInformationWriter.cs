using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol)
    {
        sourceCodeWriter.Write($"global::TUnit.Core.SourceGeneratedClassInformation.GetOrAdd(\"{namedTypeSymbol.GloballyQualified()}\", () => new global::TUnit.Core.SourceGeneratedClassInformation");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("{");    
        sourceCodeWriter.WriteLine($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Assembly = ");
        GenerateAssemblyInformation(sourceCodeWriter, context, namedTypeSymbol.ContainingAssembly);
        
        sourceCodeWriter.WriteLine($"Name = \"{namedTypeSymbol.Name}\",");
        sourceCodeWriter.WriteLine($"Namespace = \"{namedTypeSymbol.ContainingNamespace.ToDisplayString()}\",");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, namedTypeSymbol.GetAttributes());

        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Parameters = ");
        var parameters = namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters
            ?? ImmutableArray<IParameterSymbol>.Empty;
        
        if (parameters.Length == 0)
        {
            sourceCodeWriter.Write("[],");
            sourceCodeWriter.WriteLine();
        }
        else
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[");
            
            foreach (var parameter in parameters)
            {
                GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                    null);
            }

            sourceCodeWriter.WriteLine("],");
        }

        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Properties = ");
        var properties = namedTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToArray();
        
        if(properties.Length == 0)
        {
            sourceCodeWriter.Write("[],");
            sourceCodeWriter.WriteLine();
        }
        else
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[");
            foreach (var propertySymbol in properties)
            {
                GeneratePropertyInformation(sourceCodeWriter, context, propertySymbol);
            }
            sourceCodeWriter.WriteLine("],");
        }

        sourceCodeWriter.WriteLine("}),");
    }

    private static void GenerateAssemblyInformation(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, IAssemblySymbol assembly)
    {
        sourceCodeWriter.Write(
            $"global::TUnit.Core.SourceGeneratedAssemblyInformation.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.SourceGeneratedAssemblyInformation");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"Name = \"{assembly.Name}\",");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, assembly.GetAttributes());
        
        sourceCodeWriter.WriteLine("}),");
    }

    public static void GenerateMethodInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions, char suffix)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.SourceGeneratedMethodInformation");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"Type = typeof({namedTypeSymbol.GloballyQualified()}),");
        sourceCodeWriter.WriteLine($"Name = \"{methodSymbol.Name}\",");
        sourceCodeWriter.WriteLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        sourceCodeWriter.WriteLine($"ReturnType = typeof({methodSymbol.ReturnType.GloballyQualified()}),");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, methodSymbol.GetAttributes());

        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Parameters = ");
        var parameters = methodSymbol.Parameters;
        
        if (parameters.Length == 0)
        {
            sourceCodeWriter.Write("[],");
            sourceCodeWriter.WriteLine();
        }
        else
        {
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[");
            
            foreach (var parameter in parameters)
            {
                GenerateParameterInformation(sourceCodeWriter, context, parameter, ArgumentsType.ClassConstructor,
                    null);
            }

            sourceCodeWriter.WriteLine("],");
        }
            
        sourceCodeWriter.WriteTabs();
        
        sourceCodeWriter.Write("Class = ");
        GenerateClassInformation(sourceCodeWriter, context, namedTypeSymbol);

        sourceCodeWriter.Write("}");
        sourceCodeWriter.Write($"{suffix}");
        sourceCodeWriter.WriteLine();
    }

    public static void GenerateMembers(SourceCodeWriter sourceCodeWriter, GeneratorAttributeSyntaxContext context, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if(parameters.Length == 0 && property is null)
        {
            sourceCodeWriter.Write("[],");
            sourceCodeWriter.WriteLine();
            return;
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("[");

        if (property is not null)
        {
            sourceCodeWriter.WriteTabs();
            GeneratePropertyInformation(sourceCodeWriter, context, property);
        }

        foreach (var parameter in parameters)
        {
            sourceCodeWriter.WriteTabs();
            GenerateParameterInformation(sourceCodeWriter, context, parameter, argumentsType, null);
        }
        
        sourceCodeWriter.WriteLine("],");
    }

    public static void GeneratePropertyInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context, IPropertySymbol property)
    {
        sourceCodeWriter.Write("new global::TUnit.Core.SourceGeneratedPropertyInformation");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"Type = typeof({property.Type.GloballyQualified()}),");
        sourceCodeWriter.WriteLine($"Name = \"{property.Name}\",");
        sourceCodeWriter.WriteLine($"IsStatic = {property.IsStatic.ToString().ToLower()},");

        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, property.GetAttributes());

        sourceCodeWriter.WriteLine("},");
    }

    public static void GenerateParameterInformation(SourceCodeWriter sourceCodeWriter,
        GeneratorAttributeSyntaxContext context,
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

        sourceCodeWriter.Write($"new global::TUnit.Core.SourceGeneratedParameterInformation<{type}>");
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"Name = \"{parameter.Name}\",");
        
        sourceCodeWriter.WriteTabs();
        sourceCodeWriter.Write("Attributes = ");
        AttributeWriter.WriteAttributes(sourceCodeWriter, context, parameter.GetAttributes());
        
        sourceCodeWriter.WriteLine("},");
    }
}