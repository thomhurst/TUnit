using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

public struct TypeWithDataSourceProperties
{
    public INamedTypeSymbol TypeSymbol { get; init; }
    public List<PropertyWithDataSource> Properties { get; init; }
}

public sealed class TypeWithDataSourcePropertiesComparer : IEqualityComparer<TypeWithDataSourceProperties>
{
    public bool Equals(TypeWithDataSourceProperties x, TypeWithDataSourceProperties y)
    {
        // Compare based on the type symbol - this handles partial classes correctly
        return SymbolEqualityComparer.Default.Equals(x.TypeSymbol, y.TypeSymbol);
    }

    public int GetHashCode(TypeWithDataSourceProperties obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj.TypeSymbol);
    }
}
