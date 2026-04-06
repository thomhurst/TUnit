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

        // Fast path: when no argument matchers, use the per-member call counter directly.
        // Note: the count is read lock-free, then MarkCallsVerified acquires the lock.
        // Calls recorded between these two steps will be marked verified but weren't counted.
        // This is safe because verification should only run after all calls have completed.
        if (_matchers.Length == 0)
        {
            var totalCount = _engine.GetCallCountFor(_memberId);
            if (!times.Matches(totalCount))
            {
                var callsForError = _engine.GetCallsFor(_memberId);
                var expectedCall = FormatExpectedCall();
                var actualCallDescriptions = callsForError.Select(c => c.FormatCall()).ToList();
                throw new MockVerificationException(expectedCall, times, totalCount, actualCallDescriptions, message);
            }

            // Mark all calls for this member as verified (single fetch)
            if (totalCount > 0)
            {
                _engine.MarkCallsVerified(_memberId);
            }
            return;
        }

        // Iterate the internal buffer directly — no .ToArray() copy on the happy path.
        // Count first, then mark only on success to avoid corrupting prior verifications.
        var buffer = _engine.GetCallBufferFor(_memberId);
        if (buffer is null || buffer.Count == 0)
        {
            if (!times.Matches(0))
            {
                throw new MockVerificationException(FormatExpectedCall(), times, 0, [], message);
            }
            return;
        }

        var matchingCount = CountMatchingBuffer(buffer);

        if (!times.Matches(matchingCount))
        {
            var calls = _engine.GetCallsFor(_memberId);
            var expectedCall = FormatExpectedCall();
            var actualCallDescriptions = calls.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, times, matchingCount, actualCallDescriptions, message);
        }

        // Mark only after assertion passes — never leaves a partially-verified state
        MarkMatchingBuffer(buffer);
    }

    /// <inheritdoc />
    public void WasNeverCalled() => WasCalled(Times.Never, null);

    /// <inheritdoc />
    public void WasNeverCalled(string? message) => WasCalled(Times.Never, message);

    /// <inheritdoc />
    public void WasCalled() => WasCalled(Times.AtLeastOnce, null);

    /// <inheritdoc />
    public void WasCalled(string? message) => WasCalled(Times.AtLeastOnce, message);

    private int CountMatchingBuffer(CallRecordBuffer buffer)
    {
        var (items, bufferCount) = buffer.GetSnapshot();
        var count = 0;
        for (int i = 0; i < bufferCount; i++)
        {
            var record = items[i]!;
            if (_matchers.Length == 0 || MatchesArguments(record.Arguments))
            {
                count++;
            }
        }
        return count;
    }

    private void MarkMatchingBuffer(CallRecordBuffer buffer)
    {
        var (items, bufferCount) = buffer.GetSnapshot();
        for (int i = 0; i < bufferCount; i++)
        {
            var record = items[i]!;
            if (_matchers.Length == 0 || MatchesArguments(record.Arguments))
            {
                record.IsVerified = true;
            }
        }
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
