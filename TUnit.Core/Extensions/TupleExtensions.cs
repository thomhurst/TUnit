namespace TUnit.Core.Extensions;

internal static class TupleExtensions
{
    // Single value conversion - avoids array allocation for common case
    public static object?[] ToObjectArray<T>(this T value)
    {
        return [value];
    }
    
    public static object?[] ToObjectArray<T1, T2>(this (T1, T2) tuple)
    {
        return [tuple.Item1, tuple.Item2];
    }

    public static object?[] ToObjectArray<T1, T2, T3>(this (T1, T2, T3) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3];
    }

    public static object?[] ToObjectArray<T1, T2, T3, T4>(this (T1, T2, T3, T4) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4];
    }

    public static object?[] ToObjectArray<T1, T2, T3, T4, T5>(this (T1, T2, T3, T4, T5) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5];
    }
    
    public static object?[] ToObjectArray<T1, T2, T3, T4, T5, T6>(this (T1, T2, T3, T4, T5, T6) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6];
    }
    
    public static object?[] ToObjectArray<T1, T2, T3, T4, T5, T6, T7>(this (T1, T2, T3, T4, T5, T6, T7) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7];
    }
    
    public static object?[] ToObjectArray<T1, T2, T3, T4, T5, T6, T7, T8>(this (T1, T2, T3, T4, T5, T6, T7, T8) tuple)
    {
        return [tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8];
    }
}
