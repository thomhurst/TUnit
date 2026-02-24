namespace TUnit.Mocks.Diagnostics;

/// <summary>
/// Metadata about a registered setup, used for diagnostics reporting.
/// </summary>
public sealed class SetupInfo
{
    /// <summary>The member ID this setup applies to.</summary>
    public int MemberId { get; }

    /// <summary>Human-readable member name (e.g., "Add").</summary>
    public string MemberName { get; }

    /// <summary>Human-readable descriptions of each argument matcher.</summary>
    public string[] MatcherDescriptions { get; }

    /// <summary>How many times this setup was matched and invoked.</summary>
    public int InvokeCount { get; }

    public SetupInfo(int memberId, string memberName, string[] matcherDescriptions, int invokeCount)
    {
        MemberId = memberId;
        MemberName = memberName;
        MatcherDescriptions = matcherDescriptions;
        InvokeCount = invokeCount;
    }
}
