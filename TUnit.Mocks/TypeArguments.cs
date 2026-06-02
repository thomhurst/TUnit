using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Per-closed-type cache of a generic method's type-argument array, so a generic-method dispatch does
/// not allocate a fresh <see cref="Type"/>[] on every call. Generated code references
/// <c>TypeArguments.Of&lt;T&gt;.Value</c> for the common 1–4 type-argument cases; higher arities fall
/// back to a per-call <c>new Type[]</c> literal. Each <c>Value</c> is shared and read-only — it must
/// never be mutated (matching only reads it). Public for generated code access. Not intended for
/// direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeArguments
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Of<T>
    {
        public static readonly Type[] Value = { typeof(T) };
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Of<T1, T2>
    {
        public static readonly Type[] Value = { typeof(T1), typeof(T2) };
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Of<T1, T2, T3>
    {
        public static readonly Type[] Value = { typeof(T1), typeof(T2), typeof(T3) };
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Of<T1, T2, T3, T4>
    {
        public static readonly Type[] Value = { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
    }
}
