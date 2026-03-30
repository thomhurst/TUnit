using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Verifies recorded calls against expected argument matchers and call counts.
/// Created by generated verify surface classes. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CallVerificationBuilder<T> : ICallVerification where T : class
{
    private readonly MockEngine<T> _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;

    public CallVerificationBuilder(MockEngine<T> engine, int memberId, string memberName, IArgumentMatcher[] matchers)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
    }

    /// <inheritdoc />
    public void WasCalled(Times times) => WasCalled(times, null);

    /// <inheritdoc />
    public void WasCalled(Times times, string? message)
    {
        // When in ordered verification mode, record expectation and skip count checks
        if (OrderedVerification.IsCollecting)
        {
            if (times.AllowsZeroCalls)
            {
                throw new InvalidOperationException(
                    "Times.Never, Times.AtMost, and Times.Between(0, N) cannot be used inside VerifyInOrder " +
                    "because their ceiling semantics are not enforceable in ordered mode. " +
                    "Use Times.Exactly(N) or Times.AtLeast(N) instead.");
            }

            var allCalls = _engine.GetAllCalls();
            OrderedVerification.RecordExpectation(_memberId, _memberName, _matchers, times, allCalls);
            return;
        }

        // Fast path: when no argument matchers, use the per-member call counter directly
        if (_matchers.Length == 0)
        {
            var totalCount = _engine.GetCallCountFor(_memberId);
            if (!times.Matches(totalCount))
            {
                var allCallsForMember = _engine.GetCallsFor(_memberId);
                var expectedCall = FormatExpectedCall();
                var actualCallDescriptions = allCallsForMember.Select(c => c.FormatCall()).ToList();
                throw new MockVerificationException(expectedCall, times, totalCount, actualCallDescriptions, message);
            }

            // Mark all calls for this member as verified
            if (totalCount > 0)
            {
                var allCallsForMember = _engine.GetCallsFor(_memberId);
                for (int i = 0; i < allCallsForMember.Count; i++)
                {
                    allCallsForMember[i].IsVerified = true;
                }
            }
            return;
        }

        // Slow path: need to match arguments — single-pass count then mark
        var calls = _engine.GetCallsFor(_memberId);
        var matchingCount = CountMatchingCalls(calls, markVerified: false);

        if (!times.Matches(matchingCount))
        {
            var expectedCall = FormatExpectedCall();
            var actualCallDescriptions = calls.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, times, matchingCount, actualCallDescriptions, message);
        }

        // Mark matched calls as verified only after assertion passes
        CountMatchingCalls(calls, markVerified: true);
    }

    /// <inheritdoc />
    public void WasNeverCalled() => WasCalled(Times.Never, null);

    /// <inheritdoc />
    public void WasNeverCalled(string? message) => WasCalled(Times.Never, message);

    /// <inheritdoc />
    public void WasCalled() => WasCalled(Times.AtLeastOnce, null);

    /// <inheritdoc />
    public void WasCalled(string? message) => WasCalled(Times.AtLeastOnce, message);

    private int CountMatchingCalls(IReadOnlyList<CallRecord> calls, bool markVerified)
    {
        var count = 0;
        for (int i = 0; i < calls.Count; i++)
        {
            if (_matchers.Length == 0 || MatchesArguments(calls[i].Arguments))
            {
                count++;
                if (markVerified)
                {
                    calls[i].IsVerified = true;
                }
            }
        }
        return count;
    }

    private bool MatchesArguments(object?[] arguments)
    {
        if (_matchers.Length != arguments.Length)
        {
            return false;
        }

        for (int i = 0; i < _matchers.Length; i++)
        {
            if (!_matchers[i].Matches(arguments[i]))
            {
                return false;
            }
        }
        return true;
    }

    private string FormatExpectedCall()
    {
        var argDescriptions = string.Join(", ", _matchers.Select(m => m.Describe()));
        return $"{_memberName}({argDescriptions})";
    }
}
