using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

internal static class TypeExtensions
{
    public static bool IsTestClass(this INamedTypeSymbol namedTypeSymbol)
    {
        var displayString = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);

        return displayString == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix;
    }
    
    public static bool IsTuple(this INamedTypeSymbol namedTypeSymbol)
    {
        var displayString = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);

        return displayString is "global::System.Tuple" or "global::System.ValueTuple";
    }
    
    public static IEnumerable<ISymbol> GetMembersIncludingBase(this INamedTypeSymbol namedTypeSymbol, bool reverse = true)
    {
        var list = new List<ISymbol>();

        var symbol = namedTypeSymbol;

        while (symbol is not null)
        {
            if (symbol is IErrorTypeSymbol)
            {
                throw new Exception("ErrorTypeSymbol - Have you added any missing file sources to the compilation?");
            }

            if (symbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) 
                is "global::System.Object")
            {
                break;
            }
            
            list.AddRange(reverse ? symbol.GetMembers().Reverse() : symbol.GetMembers());
            symbol = symbol.BaseType;
        }
        
        if(reverse)
        {
            list.Reverse();
        }

        return list;
    }

    public static IEnumerable<ISymbol> GetSelfAndBaseTypes(this INamedTypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol;
        
        while (type != null)
        {
            yield return type;
            type = type.BaseType;
        }
    }

    public static bool IsDisposable(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.AllInterfaces.Any(x =>
            x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
            == $"global::{typeof(IDisposable).FullName}");
    }
    
    public static bool IsAsyncDisposable(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.AllInterfaces.Any(x =>
            x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
            == $"global::System.IAsyncDisposable");
    }

    public static bool IsOrInherits(this INamedTypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == typeName);
    }
}