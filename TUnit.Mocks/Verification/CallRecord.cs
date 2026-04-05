using System.ComponentModel;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Records a single method invocation. Public for generated code and verification access.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CallRecord
{
    private readonly IArgumentStore? _store;
    private object?[]? _arguments;

    /// <summary>
    /// Creates a call record with pre-boxed arguments (fallback path).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallRecord(int memberId, string memberName, object?[] arguments, long sequenceNumber)
    {
        MemberId = memberId;
        MemberName = memberName;
        _arguments = arguments;
        SequenceNumber = sequenceNumber;
    }

    /// <summary>
    /// Creates a call record with a typed argument store for deferred boxing.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallRecord(int memberId, string memberName, IArgumentStore store, long sequenceNumber)
    {
        MemberId = memberId;
        MemberName = memberName;
        _store = store;
        SequenceNumber = sequenceNumber;
    }

    /// <summary>The unique identifier for the member that was called.</summary>
    public int MemberId { get; }

    /// <summary>The name of the member that was called.</summary>
    public string MemberName { get; }

    /// <summary>The global sequence number for cross-mock ordering.</summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// The arguments passed to the call. Lazily materialized from the argument store if one was provided.
    /// </summary>
    public object?[] Arguments => _arguments ??= _store?.ToArray() ?? [];

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
