using System.ComponentModel;

namespace TUnit.Mocks.Arguments;

/// <summary>
/// Holds typed method arguments and defers boxing until <see cref="ToArray"/> is called.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IArgumentStore
{
    /// <summary>Gets the number of arguments stored.</summary>
    int Count { get; }

    /// <summary>Returns all arguments as a boxed array. Allocates on every call.</summary>
    object?[] ToArray();
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1>(T1 arg1) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <inheritdoc />
    public int Count => 1;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2>(T1 arg1, T2 arg2) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <inheritdoc />
    public int Count => 2;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <inheritdoc />
    public int Count => 3;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <summary>The fourth argument.</summary>
    public readonly T4 Arg4 = arg4;

    /// <inheritdoc />
    public int Count => 4;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3, Arg4];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <summary>The fourth argument.</summary>
    public readonly T4 Arg4 = arg4;

    /// <summary>The fifth argument.</summary>
    public readonly T5 Arg5 = arg5;

    /// <inheritdoc />
    public int Count => 5;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3, Arg4, Arg5];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <summary>The fourth argument.</summary>
    public readonly T4 Arg4 = arg4;

    /// <summary>The fifth argument.</summary>
    public readonly T5 Arg5 = arg5;

    /// <summary>The sixth argument.</summary>
    public readonly T6 Arg6 = arg6;

    /// <inheritdoc />
    public int Count => 6;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3, Arg4, Arg5, Arg6];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <summary>The fourth argument.</summary>
    public readonly T4 Arg4 = arg4;

    /// <summary>The fifth argument.</summary>
    public readonly T5 Arg5 = arg5;

    /// <summary>The sixth argument.</summary>
    public readonly T6 Arg6 = arg6;

    /// <summary>The seventh argument.</summary>
    public readonly T7 Arg7 = arg7;

    /// <inheritdoc />
    public int Count => 7;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7];
}

/// <inheritdoc cref="IArgumentStore"/>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ArgumentStore<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) : IArgumentStore
{
    /// <summary>The first argument.</summary>
    public readonly T1 Arg1 = arg1;

    /// <summary>The second argument.</summary>
    public readonly T2 Arg2 = arg2;

    /// <summary>The third argument.</summary>
    public readonly T3 Arg3 = arg3;

    /// <summary>The fourth argument.</summary>
    public readonly T4 Arg4 = arg4;

    /// <summary>The fifth argument.</summary>
    public readonly T5 Arg5 = arg5;

    /// <summary>The sixth argument.</summary>
    public readonly T6 Arg6 = arg6;

    /// <summary>The seventh argument.</summary>
    public readonly T7 Arg7 = arg7;

    /// <summary>The eighth argument.</summary>
    public readonly T8 Arg8 = arg8;

    /// <inheritdoc />
    public int Count => 8;

    /// <inheritdoc />
    public object?[] ToArray() => [Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8];
}
