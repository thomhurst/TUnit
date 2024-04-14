using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;

namespace TUnit.Engine.SourceGenerator.Extensions;

internal static class TypeExtensions
{
    public static bool IsTestClass(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            is WellKnownFullyQualifiedClassNames.TestAttribute
            or WellKnownFullyQualifiedClassNames.DataDrivenTestAttribute
            or WellKnownFullyQualifiedClassNames.DataSourceDrivenTestAttribute
            or WellKnownFullyQualifiedClassNames.CombinativeTestAttribute;
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