using System.ComponentModel;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Thread-local storage for out/ref parameter assignments from the last matched setup.
/// The generated mock implementation reads from this after calling the engine.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class OutRefContext
{
    /// <summary>
    /// Reserved index used to store the return value for span-returning methods.
    /// Parameter indices are always &gt;= 0, so -1 is safe from collision.
    /// </summary>
    public const int SpanReturnValueIndex = -1;

    [ThreadStatic]
    private static Dictionary<int, object?>? _assignments;

    /// <summary>Sets the assignments from a matched setup.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Set(Dictionary<int, object?>? assignments) => _assignments = assignments;

    /// <summary>Consumes and returns the assignments. Returns null if none were set.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Dictionary<int, object?>? Consume()
    {
        var result = _assignments;
        _assignments = null;
        return result;
    }
}
