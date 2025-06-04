namespace TUnit.Core.Extensions;

internal static class TupleExtensions
{
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
}
