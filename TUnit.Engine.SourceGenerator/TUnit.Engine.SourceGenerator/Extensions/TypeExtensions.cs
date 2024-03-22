using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class TypeExtensions
{
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
            == $"global::{typeof(IAsyncDisposable).FullName}");
    }
}