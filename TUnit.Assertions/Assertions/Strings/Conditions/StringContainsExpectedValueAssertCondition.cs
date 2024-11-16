using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class StringContainsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to contain {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    () => "it was null");
        }

        return AssertionResult
            .FailIf(
                () => !actualValue.Contains(expectedValue!, stringComparison),
                () => $"it was not found. {MessageSuffix()}");
    }

    private string MessageSuffix()
    {
        // if (ExpectedValue?.Length > 100)
        // {
        //     return string.Empty;
        // }
        
        return $"Closest match is {LevenshteinDistance.FindClosestSubstring(ActualValue!, ExpectedValue)}";
    }
}

internal class LevenshteinDistance
{
    public static string FindClosestSubstring(string? text, string? pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
        {
            return string.Empty;
        }

        var matchingConsecutiveCount = 0;
        var bestIndex = 0;

        var c = pattern[0];
        var indexes = text.Select((b, i) => b.Equals(c) ? i : -1).Where(i => i != -1).ToArray();
        
        foreach (var index in indexes)
        {
            var consecutiveCount = 0;
            
            for (var i = 0; i < pattern.Length; i++)
            {
                if (text[index + i] == pattern[i])
                {
                    consecutiveCount++;
                }
                else
                {
                    break;
                }
            }

            if (consecutiveCount > matchingConsecutiveCount)
            {
                matchingConsecutiveCount = consecutiveCount;
                bestIndex = index;
            }
        }

        return text.Substring(bestIndex, matchingConsecutiveCount + 25);
    }
}