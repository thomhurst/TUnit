using System.ComponentModel;

namespace TUnit.Mock.Verification;

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

    public string FormatCall()
    {
        var args = string.Join(", ", Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{MemberName}({args})";
    }
}
