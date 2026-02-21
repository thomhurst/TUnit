using System.ComponentModel;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Records a single method invocation. Public for generated code and verification access.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record CallRecord(
    int MemberId,
    string MemberName,
    object?[] Arguments,
    long SequenceNumber
)
{
    /// <summary>
    /// Backing field for <see cref="IsVerified"/>. Exposed for <see cref="System.Threading.Volatile"/> access.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsVerifiedField;

    /// <summary>
    /// Whether this call has been matched by a verification statement.
    /// Used by <see cref="Mock{T}.VerifyNoOtherCalls"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsVerified
    {
        get => Volatile.Read(ref IsVerifiedField);
        internal set => Volatile.Write(ref IsVerifiedField, value);
    }

    /// <summary>
    /// Backing field for <see cref="IsUnmatched"/>. Exposed for <see cref="System.Threading.Volatile"/> access.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsUnmatchedField;

    /// <summary>
    /// Whether this call had no matching setup (fell through to default behavior).
    /// Used by <see cref="Diagnostics.MockDiagnostics"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsUnmatched
    {
        get => Volatile.Read(ref IsUnmatchedField);
        internal set => Volatile.Write(ref IsUnmatchedField, value);
    }

    public string FormatCall()
    {
        var args = string.Join(", ", Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{MemberName}({args})";
    }
}
