using System;
using System.Collections.Immutable;
using System.Linq;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class ImmutableArrayExtensions
{
    public static T? SafeFirstOrDefault<T>(this ImmutableArray<T> immutableArray)
    {
        if (immutableArray.IsDefaultOrEmpty)
        {
            return default;
        }

        return immutableArray.First();
    }
    
    public static T? SafeFirstOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
    {
        if (immutableArray.IsDefaultOrEmpty)
        {
            return default;
        }

        return immutableArray.First(predicate);
    }
}