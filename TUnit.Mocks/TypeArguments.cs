using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Per-closed-type cache of a generic method's type-argument array, so a generic-method dispatch does
/// not allocate a fresh <see cref="Type"/>[] on every call. The arrays are immutable in practice
/// (only read by matching/verification), so sharing one instance per closed generic instantiation is
/// safe. Generated code references <c>TypeArguments.Of&lt;T&gt;.Value</c> for the common 1–2 type-arg
/// cases; higher arities fall back to a per-call <c>new Type[]</c> literal. Public for generated code
/// access. Not intended for direct use.
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
}
