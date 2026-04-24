using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions;

/// <summary>
/// Infrastructure wrapper used by the <c>Assert.That(string?)</c> overload to prevent user types
/// with an implicit conversion to <see cref="string"/> from silently binding to it.
/// </summary>
/// <remarks>
/// C# allows only one user-defined implicit conversion in a single conversion chain. A value
/// whose static type is a user record with <c>implicit operator string</c> would, if the
/// parameter type were <c>string?</c>, invoke that operator at the call site — producing a
/// <see cref="NullReferenceException"/> when the receiver is null, inside user code, before the
/// assertion method ever runs (see issue #5692). By accepting <see cref="StringValue"/> — which
/// declares an implicit conversion only from <c>string?</c> — a value of that user type cannot
/// form a valid conversion chain (<c>Id → string → StringValue</c> would require two
/// user-defined steps) and is routed to the generic <c>Assert.That&lt;TValue&gt;</c> overload
/// instead.
/// <para>
/// Not intended for direct use. Construction happens implicitly from <see cref="string"/>.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct StringValue
{
    public string? Value { get; }

    private StringValue(string? value) => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringValue(string? value) => new(value);
}
