using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

internal static class TypeExtensions
{
    public static bool IsTestClass(this INamedTypeSymbol namedTypeSymbol)
    {
        var displayString = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);

        return displayString == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix
               || displayString == WellKnownFullyQualifiedClassNames.DataDrivenTestAttribute.WithGlobalPrefix
               || displayString == WellKnownFullyQualifiedClassNames.DataSourceDrivenTestAttribute.WithGlobalPrefix
               || displayString == WellKnownFullyQualifiedClassNames.CombinativeTestAttribute.WithGlobalPrefix;
    }
    
    public static IEnumerable<ISymbol> GetMembersIncludingBase(this INamedTypeSymbol namedTypeSymbol)
    {
        var list = new List<ISymbol>();

        var symbol = namedTypeSymbol;

        while (symbol is not null and not IErrorTypeSymbol)
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