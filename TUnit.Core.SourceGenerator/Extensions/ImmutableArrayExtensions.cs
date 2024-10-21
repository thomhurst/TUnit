using System.Collections.Immutable;

namespace TUnit.Core.SourceGenerator.Extensions;

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

        return immutableArray.FirstOrDefault(predicate);
    }
    
    public static T? SafeFirstOrDefault<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is ImmutableArray<T> immutableArray)
        {
            return immutableArray.SafeFirstOrDefault();
        }

        return enumerable.FirstOrDefault();
    }

    public static T? SafeFirstOrDefault<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
    {
        if (enumerable is ImmutableArray<T> immutableArray)
        {
            return immutableArray.SafeFirstOrDefault(predicate);
        }

        return enumerable.FirstOrDefault(predicate);
    }
}