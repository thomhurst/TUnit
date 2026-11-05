using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class AttributeTypeExpression
{
    // A constant assembly-qualified name lets the trimmer/AOT compiler resolve and retain the
    // type, without requiring generated top-level C# to have access to its declaration.
    public static string Format(ITypeSymbol type) =>
        $"global::System.Type.GetType({SymbolDisplay.FormatLiteral(AssemblyQualifiedName(type), true)}, throwOnError: true)!";

    private static string AssemblyQualifiedName(ITypeSymbol type) =>
        $"{MetadataName(type)}, {GetAssembly(type).Identity}";

    private static IAssemblySymbol GetAssembly(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => GetAssembly(array.ElementType),
        IPointerTypeSymbol pointer => GetAssembly(pointer.PointedAtType),
        _ => type.ContainingAssembly
    };

    private static string MetadataName(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return MetadataName(array.ElementType) + "[" + new string(',', array.Rank - 1) + "]";
        }

        if (type is IPointerTypeSymbol pointer)
        {
            return MetadataName(pointer.PointedAtType) + "*";
        }

        var named = (INamedTypeSymbol)type;
        var chain = new Stack<INamedTypeSymbol>();
        for (var current = named; current is not null; current = current.ContainingType)
        {
            chain.Push(current);
        }

        var name = named.ContainingNamespace.IsGlobalNamespace
            ? ""
            : named.ContainingNamespace.ToDisplayString() + ".";
        name += string.Join("+", chain.Select(t => t.MetadataName));
        if (!named.IsUnboundGenericType)
        {
            var arguments = chain.SelectMany(t => t.TypeArguments).ToArray();
            if (arguments.Length > 0)
            {
                name += "[" + string.Join(",", arguments.Select(t => "[" + AssemblyQualifiedName(t) + "]")) + "]";
            }
        }

        return name;
    }
}
