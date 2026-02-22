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
    internal bool IsVerifiedField;

    /// <summary>
    /// Whether this call has been matched by a verification statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsVerified
    {
        get => Volatile.Read(ref IsVerifiedField);
        internal set => Volatile.Write(ref IsVerifiedField, value);
    }

    internal bool IsUnmatchedField;

    /// <summary>
    /// Whether this call had no matching setup (fell through to default behavior).
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
