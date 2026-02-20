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
    DateTime Timestamp,
    long SequenceNumber
)
{
    public string FormatCall()
    {
        var args = string.Join(", ", Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{MemberName}({args})";
    }
}
