namespace TUnit.Mocks.Setup.Behaviors;

/// <summary>
/// Typed behavior dispatch interfaces. ExecuteBehavior checks for these after IArgumentFreeBehavior,
/// enabling behaviors to receive typed arguments without boxing into object?[].
/// Currently implemented by TypedCallbackBehavior; extensible for future typed return behaviors
/// (e.g. a TypedComputedReturnBehavior that takes Func&lt;T1, TReturn&gt;).
/// </summary>
/// <remarks>
/// Intentionally internal: the typed dispatch is tightly coupled to the source generator's
/// knowledge of parameter arity — only generated code knows the concrete types at compile time.
/// <see cref="IArgumentFreeBehavior"/> is public because any behavior can opt in without type knowledge.
/// </remarks>
internal interface ITypedBehavior<T1>
{
    object? Execute(T1 arg1);
}

internal interface ITypedBehavior<T1, T2>
{
    object? Execute(T1 arg1, T2 arg2);
}

internal interface ITypedBehavior<T1, T2, T3>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3);
}

internal interface ITypedBehavior<T1, T2, T3, T4>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

internal interface ITypedBehavior<T1, T2, T3, T4, T5>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}

internal interface ITypedBehavior<T1, T2, T3, T4, T5, T6>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
}

internal interface ITypedBehavior<T1, T2, T3, T4, T5, T6, T7>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
}

internal interface ITypedBehavior<T1, T2, T3, T4, T5, T6, T7, T8>
{
    object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
}

// ── ARITY COUPLING (1–8) ──────────────────────────────────────────────
// If you add an arity (e.g. T9), you MUST also update:
//   - ITypedBehavior<T1...T9> above
//   - ExecuteBehavior<T1...T9> in MockEngine.Typed.cs
//   - Callback<T1...T9> in MethodSetupBuilder.cs and VoidMethodSetupBuilder.cs
//   - MaxTypedParams in MockMembersBuilder.cs (source generator)
// ──────────────────────────────────────────────────────────────────────

internal sealed class TypedCallbackBehavior<T1>(Action<T1> callback) : IBehavior, ITypedBehavior<T1>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 1) throw new ArgumentException($"Expected at least 1 argument, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!);
        return null;
    }

    public object? Execute(T1 arg1) { callback(arg1); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2>(Action<T1, T2> callback) : IBehavior, ITypedBehavior<T1, T2>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 2) throw new ArgumentException($"Expected at least 2 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2) { callback(arg1, arg2); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3>(Action<T1, T2, T3> callback) : IBehavior, ITypedBehavior<T1, T2, T3>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 3) throw new ArgumentException($"Expected at least 3 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3) { callback(arg1, arg2, arg3); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callback) : IBehavior, ITypedBehavior<T1, T2, T3, T4>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 4) throw new ArgumentException($"Expected at least 4 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!, (T4)arguments[3]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4) { callback(arg1, arg2, arg3, arg4); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callback) : IBehavior, ITypedBehavior<T1, T2, T3, T4, T5>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 5) throw new ArgumentException($"Expected at least 5 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!, (T4)arguments[3]!, (T5)arguments[4]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) { callback(arg1, arg2, arg3, arg4, arg5); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callback) : IBehavior, ITypedBehavior<T1, T2, T3, T4, T5, T6>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 6) throw new ArgumentException($"Expected at least 6 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!, (T4)arguments[3]!, (T5)arguments[4]!, (T6)arguments[5]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) { callback(arg1, arg2, arg3, arg4, arg5, arg6); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> callback) : IBehavior, ITypedBehavior<T1, T2, T3, T4, T5, T6, T7>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 7) throw new ArgumentException($"Expected at least 7 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!, (T4)arguments[3]!, (T5)arguments[4]!, (T6)arguments[5]!, (T7)arguments[6]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) { callback(arg1, arg2, arg3, arg4, arg5, arg6, arg7); return null; }
}

internal sealed class TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> callback) : IBehavior, ITypedBehavior<T1, T2, T3, T4, T5, T6, T7, T8>
{
    public object? Execute(object?[] arguments)
    {
        if (arguments.Length < 8) throw new ArgumentException($"Expected at least 8 arguments, got {arguments.Length}.", nameof(arguments));
        callback((T1)arguments[0]!, (T2)arguments[1]!, (T3)arguments[2]!, (T4)arguments[3]!, (T5)arguments[4]!, (T6)arguments[5]!, (T7)arguments[6]!, (T8)arguments[7]!);
        return null;
    }

    public object? Execute(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) { callback(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); return null; }
}
