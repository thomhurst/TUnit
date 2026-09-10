using System.ComponentModel;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Marker type wrapping a raw return value (e.g., a Task or ValueTask) that should
/// bypass the engine's normal type coercion. When the engine encounters this as a
/// behavior result, it stores the inner value in <see cref="RawReturnContext"/>
/// for the generated code to consume directly.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawReturn
{
    public object? Value { get; }

    public RawReturn(object? value) => Value = value;
}

/// <summary>
/// Thread-local storage for raw return values from <see cref="RawReturn"/> behaviors.
/// The generated mock implementation reads from this after calling the engine,
/// allowing async methods to return pre-built Task/ValueTask instances directly
/// (e.g., from a <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/>).
/// Public for generated code access. Not intended for direct use.
/// </summary>
/// <remarks>
/// IMPORTANT: RawReturnContext must be consumed synchronously in the same execution
/// context as HandleCall*/TryHandleCall*. No await may appear between the engine
/// dispatch call and TryConsume in the generated code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RawReturnContext
{
    [ThreadStatic]
    private static RawReturn? _pending;

    /// <summary>Stores a <see cref="RawReturn"/> for the generated code to consume. Accepts the marker directly to avoid re-wrapping.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Set(RawReturn raw) => _pending = raw;

    /// <summary>Consumes and returns the raw return value, if one was set. Clears the slot.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool TryConsume(out object? value)
    {
        if (_pending is { } raw)
        {
            value = raw.Value;
            _pending = null;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Clears any stale value from a previous call. Called at dispatch entry to prevent leaks.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Clear() => _pending = null;
}
