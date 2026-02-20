using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Verification;

/// <summary>
/// Captures verification expectations in order and validates that the corresponding
/// actual calls were made in chronological sequence.
/// </summary>
public static class OrderedVerification
{
    [ThreadStatic]
    private static List<OrderedCallExpectation>? _expectations;

    /// <summary>
    /// Returns true when an ordered verification is currently being collected.
    /// Used by <see cref="CallVerificationBuilder{T}"/> to switch into recording mode.
    /// </summary>
    internal static bool IsCollecting => _expectations is not null;

    /// <summary>
    /// Records an expectation during ordered verification collection.
    /// Called by <see cref="CallVerificationBuilder{T}.WasCalled()"/> and overloads.
    /// </summary>
    internal static void RecordExpectation(int memberId, string memberName, IArgumentMatcher[] matchers, IReadOnlyList<CallRecord> allCalls)
    {
        _expectations?.Add(new OrderedCallExpectation(memberId, memberName, matchers, allCalls));
    }

    /// <summary>
    /// Executes the verification actions, collecting expected call order,
    /// then validates that actual calls occurred in the expected sequence.
    /// </summary>
    public static void Verify(Action verificationActions)
    {
        _expectations = new List<OrderedCallExpectation>();
        try
        {
            verificationActions();
            ValidateOrder();
        }
        finally
        {
            _expectations = null;
        }
    }

    private static void ValidateOrder()
    {
        var expectations = _expectations!;

        if (expectations.Count == 0)
        {
            return;
        }

        // For each expectation, find the earliest unassigned matching call
        var assignedSequences = new HashSet<long>();
        var assignedCalls = new List<(OrderedCallExpectation Expectation, CallRecord Call)>();

        foreach (var expectation in expectations)
        {
            var matchingCalls = FindMatchingCalls(expectation);

            CallRecord? bestMatch = null;
            foreach (var call in matchingCalls.OrderBy(c => c.SequenceNumber))
            {
                if (!assignedSequences.Contains(call.SequenceNumber))
                {
                    bestMatch = call;
                    break;
                }
            }

            if (bestMatch is null)
            {
                var expectedCallDesc = FormatExpectedCall(expectation);
                throw new MockVerificationException(
                    $"Ordered verification failed.\n" +
                    $"  No matching call found for: {expectedCallDesc}\n" +
                    $"  Expected order position: {assignedCalls.Count + 1}");
            }

            assignedSequences.Add(bestMatch.SequenceNumber);
            assignedCalls.Add((expectation, bestMatch));
        }

        // Validate that assigned calls are in strictly increasing sequence order
        for (int i = 1; i < assignedCalls.Count; i++)
        {
            var prev = assignedCalls[i - 1];
            var curr = assignedCalls[i];

            if (curr.Call.SequenceNumber < prev.Call.SequenceNumber)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Ordered verification failed.");
                sb.AppendLine("  Expected calls in this order:");
                for (int j = 0; j < assignedCalls.Count; j++)
                {
                    var marker = j == i ? " <-- out of order" : "";
                    sb.AppendLine($"    {j + 1}. {FormatExpectedCall(assignedCalls[j].Expectation)}{marker}");
                }
                sb.AppendLine("  Actual call order (by sequence):");
                // Collect all relevant calls, sort by sequence
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
    IReadOnlyList<CallRecord> AllCalls
);
