using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Captures verification expectations in order and validates that the corresponding
/// actual calls were made in chronological sequence.
/// </summary>
public static class OrderedVerification
{
    private static readonly AsyncLocal<List<OrderedCallExpectation>?> _expectations = new();

    /// <summary>
    /// Returns true when an ordered verification is currently being collected.
    /// Used by <see cref="CallVerificationBuilder{T}"/> to switch into recording mode.
    /// </summary>
    internal static bool IsCollecting => _expectations.Value is not null;

    /// <summary>
    /// Records an expectation during ordered verification collection.
    /// Called by <see cref="CallVerificationBuilder{T}.WasCalled()"/> and overloads.
    /// </summary>
    internal static void RecordExpectation(int memberId, string memberName, IArgumentMatcher[] matchers, Times times, IReadOnlyList<CallRecord> allCalls)
    {
        _expectations.Value?.Add(new OrderedCallExpectation(memberId, memberName, matchers, times, allCalls));
    }

    /// <summary>
    /// Executes the verification actions, collecting expected call order,
    /// then validates that actual calls occurred in the expected sequence.
    /// </summary>
    public static void Verify(Action verificationActions)
    {
        _expectations.Value = new List<OrderedCallExpectation>();
        try
        {
            verificationActions();
            ValidateOrder();
        }
        finally
        {
            _expectations.Value = null;
        }
    }

    private static void ValidateOrder()
    {
        var expectations = _expectations.Value!;

        if (expectations.Count == 0)
        {
            return;
        }

        // For each expectation, find the required number of matching calls based on Times
        var assignedSequences = new HashSet<long>();
        var assignedCalls = new List<(OrderedCallExpectation Expectation, CallRecord Call)>();

        foreach (var expectation in expectations)
        {
            var matchingCalls = FindMatchingCalls(expectation);
            var availableCalls = matchingCalls
                .Where(c => !assignedSequences.Contains(c.SequenceNumber))
                .OrderBy(c => c.SequenceNumber)
                .ToList();

            // Collect all matching calls for this expectation to validate Times
            var assigned = new List<CallRecord>();
            foreach (var call in availableCalls)
            {
                assigned.Add(call);
                assignedSequences.Add(call.SequenceNumber);

                // Stop if we've found enough for an exact match
                if (expectation.Times.Matches(assigned.Count))
                {
                    break;
                }
            }

            if (!expectation.Times.Matches(assigned.Count))
            {
                var expectedCallDesc = FormatExpectedCall(expectation);
                throw new MockVerificationException(
                    $"Ordered verification failed.\n" +
                    $"  Expected: {expectedCallDesc} to be called {expectation.Times}\n" +
                    $"  Actual: {assigned.Count} matching call(s) found\n" +
                    $"  Expected order position: {assignedCalls.Count + 1}");
            }

            // Use the last assigned call for ordering validation
            foreach (var call in assigned)
            {
                assignedCalls.Add((expectation, call));
            }
        }

        // Group assigned calls by expectation and validate ordering between groups.
        // Within each expectation, calls may be interleaved with other expectations' calls.
        // We only require: max(expectation[i].sequences) < min(expectation[i+1].sequences)
        var expectationGroups = new List<(OrderedCallExpectation Expectation, long MinSeq, long MaxSeq)>();
        var currentExpectation = assignedCalls[0].Expectation;
        long minSeq = assignedCalls[0].Call.SequenceNumber;
        long maxSeq = assignedCalls[0].Call.SequenceNumber;

        for (int i = 1; i < assignedCalls.Count; i++)
        {
            if (ReferenceEquals(assignedCalls[i].Expectation, currentExpectation))
            {
                var seq = assignedCalls[i].Call.SequenceNumber;
                if (seq < minSeq) minSeq = seq;
                if (seq > maxSeq) maxSeq = seq;
            }
            else
            {
                expectationGroups.Add((currentExpectation, minSeq, maxSeq));
                currentExpectation = assignedCalls[i].Expectation;
                minSeq = assignedCalls[i].Call.SequenceNumber;
                maxSeq = assignedCalls[i].Call.SequenceNumber;
            }
        }
        expectationGroups.Add((currentExpectation, minSeq, maxSeq));

        for (int i = 1; i < expectationGroups.Count; i++)
        {
            var prev = expectationGroups[i - 1];
            var curr = expectationGroups[i];

            if (curr.MinSeq < prev.MaxSeq)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Ordered verification failed.");
                sb.AppendLine("  Expected calls in this order:");
                for (int j = 0; j < expectationGroups.Count; j++)
                {
                    var marker = j == i ? " <-- out of order" : "";
                    sb.AppendLine($"    {j + 1}. {FormatExpectedCall(expectationGroups[j].Expectation)}{marker}");
                }
                sb.AppendLine("  Actual call order (by sequence):");
                var allRelevant = assignedCalls
                    .Select(a => a.Call)
                    .OrderBy(c => c.SequenceNumber)
                    .ToList();
                for (int j = 0; j < allRelevant.Count; j++)
                {
                    sb.AppendLine($"    {j + 1}. {allRelevant[j].FormatCall()} (seq #{allRelevant[j].SequenceNumber})");
                }

                throw new MockVerificationException(sb.ToString());
            }
        }

        // Mark all successfully ordered calls as verified for VerifyNoOtherCalls
        foreach (var (_, call) in assignedCalls)
        {
            call.IsVerified = true;
        }
    }

    private static List<CallRecord> FindMatchingCalls(OrderedCallExpectation expectation)
    {
        var result = new List<CallRecord>();
        foreach (var call in expectation.AllCalls)
        {
            if (call.MemberId == expectation.MemberId && MatchesArguments(call.Arguments, expectation.Matchers))
            {
                result.Add(call);
            }
        }
        return result;
    }

    private static bool MatchesArguments(object?[] arguments, IArgumentMatcher[] matchers)
    {
        if (matchers.Length == 0)
        {
            return true;
        }

        if (matchers.Length != arguments.Length)
        {
            return false;
        }

        for (int i = 0; i < matchers.Length; i++)
        {
            if (!matchers[i].Matches(arguments[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static string FormatExpectedCall(OrderedCallExpectation expectation)
    {
        if (expectation.Matchers.Length == 0)
        {
            return $"{expectation.MemberName}()";
        }

        var argDescriptions = string.Join(", ", expectation.Matchers.Select(m => m.Describe()));
        return $"{expectation.MemberName}({argDescriptions})";
    }
}

/// <summary>
/// Represents a single expected call in an ordered verification sequence.
/// </summary>
internal sealed record OrderedCallExpectation(
    int MemberId,
    string MemberName,
    IArgumentMatcher[] Matchers,
    Times Times,
    IReadOnlyList<CallRecord> AllCalls
);
