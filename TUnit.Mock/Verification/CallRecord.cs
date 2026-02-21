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
    /// Whether this call has been matched by a verification statement.
    /// Used by <see cref="Mock{T}.VerifyNoOtherCalls"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsVerified { get; internal set; }

    public string FormatCall()
    {
        var args = string.Join(", ", Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{MemberName}({args})";
    }
}
