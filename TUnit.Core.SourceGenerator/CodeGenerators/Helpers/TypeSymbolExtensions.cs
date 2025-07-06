using Microsoft.CodeAnalysis;
using System.Linq;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypeSymbolExtensions
{
    public static bool HasParameterizedConstructor(this ITypeSymbol typeSymbol)
    {
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic);

        return constructors.Any(c => c.Parameters.Length > 0);
    }
}