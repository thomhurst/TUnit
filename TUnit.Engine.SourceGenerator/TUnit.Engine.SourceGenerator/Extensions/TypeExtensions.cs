using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class TypeExtensions
{
    public static IEnumerable<ISymbol> GetMembersIncludingBase(this INamedTypeSymbol namedTypeSymbol)
    {
        var list = new List<ISymbol>();

        var symbol = namedTypeSymbol;

        while (symbol != null)
        {
            list.AddRange(symbol.GetMembers());
            symbol = symbol.BaseType;
        }
        
        list.Reverse();

        return list;
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
}