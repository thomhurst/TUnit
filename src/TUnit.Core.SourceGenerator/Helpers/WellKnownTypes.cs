using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

public class WellKnownTypes(Compilation compilation)
{
    private readonly Dictionary<string, INamedTypeSymbol?> _cachedTypes = new();
    private readonly Dictionary<INamedTypeSymbol, string> _cachedToDisplayString = new(SymbolEqualityComparer.Default);

    public string GetDisplayString(INamedTypeSymbol type)
    {
        if (_cachedToDisplayString.TryGetValue(type, out var displayString))
        {
            return displayString;
        }

        displayString = type.ToDisplayString();
        _cachedToDisplayString.Add(type, displayString);

        return displayString;
    }

    public INamedTypeSymbol Get<T>() => Get(typeof(T));

    public INamedTypeSymbol Get(Type type)
    {
        if (type.IsConstructedGenericType)
        {
            type = type.GetGenericTypeDefinition();
        }

        return Get(type.FullName ?? throw new InvalidOperationException("Could not get name of type " + type));
    }

    public INamedTypeSymbol? TryGet(string typeFullName)
    {
        if (_cachedTypes.TryGetValue(typeFullName, out var typeSymbol))
        {
            return typeSymbol;
        }

        typeSymbol = compilation.GetTypeByMetadataName(typeFullName);
        _cachedTypes.Add(typeFullName, typeSymbol);

        return typeSymbol;
    }

    private INamedTypeSymbol Get(string typeFullName) =>
        TryGet(typeFullName) ?? throw new InvalidOperationException("Could not get type " + typeFullName);
}
