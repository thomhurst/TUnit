using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TestInformationGenerator
{
    public static void GenerateTestInformation(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("new global::TUnit.Core.MethodMetadata");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"Type = typeof({GetSafeTypeDisplayString(typeSymbol)}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(typeSymbol)},");
        writer.AppendLine($"Name = \"{methodSymbol.Name}\",");
        writer.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.AppendLine($"ReturnType = typeof({GetSafeTypeDisplayString(methodSymbol.ReturnType)}),");
        writer.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
        
        writer.AppendLine("Parameters = ");
        GenerateParameters(writer, methodSymbol);
        
        writer.AppendLine("Class = ");
        GenerateClassMetadata(writer, typeSymbol);
        
        writer.Unindent();
        writer.AppendLine("}");
    }
    
    private static void GenerateParameters(CodeWriter writer, IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters;
        
        if (parameters.Length == 0)
        {
            writer.AppendLine("[],");
            return;
        }
        
        writer.AppendLine("[");
        writer.Indent();
        
        foreach (var parameter in parameters)
        {
            var type = GetSafeTypeDisplayString(parameter.Type);
            
            writer.AppendLine($"new global::TUnit.Core.ParameterMetadata(typeof({type}))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"Name = \"{parameter.Name}\",");
            writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},");
            writer.AppendLine("ReflectionInfo = null!");
            writer.Unindent();
            writer.AppendLine("},");
        }
        
        writer.Unindent();
        writer.AppendLine("],");
    }
    
    private static void GenerateClassMetadata(CodeWriter writer, INamedTypeSymbol typeSymbol)
    {
        var qualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeTypeName = GetSafeTypeDisplayString(typeSymbol);
        
        writer.AppendLine($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{qualifiedName}\", () => new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"Type = typeof({safeTypeName}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(typeSymbol)},");
        writer.AppendLine($"Name = \"{typeSymbol.Name}\",");
        writer.AppendLine($"Namespace = \"{typeSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        
        writer.AppendLine("Assembly = ");
        GenerateAssemblyMetadata(writer, typeSymbol.ContainingAssembly);
        
        writer.AppendLine("Parameters = [],");
        writer.AppendLine("Properties = [],");
        writer.AppendLine("Parent = null");
        
        writer.Unindent();
        writer.AppendLine("})");
    }
    
    private static void GenerateAssemblyMetadata(CodeWriter writer, IAssemblySymbol assembly)
    {
        writer.AppendLine($"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.AssemblyMetadata");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"Name = \"{assembly.Name}\"");
        writer.Unindent();
        writer.AppendLine("}),");
    }
    
    /// <summary>
    /// Gets a safe type display string for typeof() expressions, replacing generic type parameters with object placeholders.
    /// </summary>
    private static string GetSafeTypeDisplayString(ITypeSymbol type)
    {
        if (CodeGenerationHelpers.ContainsTypeParameter(type))
        {
            return "object";
        }
        
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}