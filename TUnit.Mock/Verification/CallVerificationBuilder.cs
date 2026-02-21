using System.ComponentModel;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Verification;

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
    public void WasCalled(Times times)
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

        var allCallsForMember = _engine.GetCallsFor(_memberId);

        // Filter by matchers
        var matchingCalls = FilterByMatchers(allCallsForMember);
        var matchingCount = matchingCalls.Count;

        if (!times.Matches(matchingCount))
        {
            var expectedCall = FormatExpectedCall();
            var actualCallDescriptions = allCallsForMember.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, times, matchingCount, actualCallDescriptions);
        }

        // Mark matched calls as verified for VerifyNoOtherCalls
        foreach (var call in matchingCalls)
        {
            call.IsVerified = true;
        }
    }

    /// <inheritdoc />
    public void WasNeverCalled() => WasCalled(Times.Never);

    /// <inheritdoc />
    public void WasCalled() => WasCalled(Times.AtLeastOnce);

    private List<CallRecord> FilterByMatchers(IReadOnlyList<CallRecord> calls)
    {
        if (_matchers.Length == 0)
        {
            return calls.ToList();
        }

        var result = new List<CallRecord>();
        foreach (var call in calls)
        {
            if (MatchesArguments(call.Arguments))
            {
                result.Add(call);
            }
        }
        return result;
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
