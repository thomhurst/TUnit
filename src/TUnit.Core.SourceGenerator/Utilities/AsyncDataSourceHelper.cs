using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Helper methods for async data source conversion
/// </summary>
internal static class AsyncDataSourceHelper
{
    /// <summary>
    /// Determines if a method represents an async data source
    /// </summary>
    public static bool IsAsyncDataSource(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        // Check for Task<IEnumerable<T>>
        if (returnType is INamedTypeSymbol { Name: "Task", IsGenericType: true } namedType)
        {
            var innerType = namedType.TypeArguments.FirstOrDefault();
            if (innerType is INamedTypeSymbol innerNamed && IsEnumerableType(innerNamed))
            {
                return true;
            }
        }

        // Check for ValueTask<IEnumerable<T>>
        if (returnType is INamedTypeSymbol { Name: "ValueTask", IsGenericType: true } valueTaskType)
        {
            var innerType = valueTaskType.TypeArguments.FirstOrDefault();
            if (innerType is INamedTypeSymbol innerNamed && IsEnumerableType(innerNamed))
            {
                return true;
            }
        }

        // Check for IAsyncEnumerable<T>
        if (returnType is INamedTypeSymbol { Name: "IAsyncEnumerable", IsGenericType: true })
        {
            return true;
        }

        return false;
    }

    private static bool IsEnumerableType(INamedTypeSymbol type)
    {
        return type is { Name: "IEnumerable", IsGenericType: true };
    }
}
