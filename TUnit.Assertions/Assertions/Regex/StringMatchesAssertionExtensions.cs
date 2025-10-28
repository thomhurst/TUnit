using System.Text.RegularExpressions;
using TUnit.Assertions.Conditions;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Extension methods for StringMatchesAssertion to access regex match results.
/// </summary>
public static class StringMatchesAssertionExtensions
{
    /// <summary>
    /// Gets the regex match result after asserting the string matches.
    /// Provides access to capture groups, match position, and length.
    /// </summary>
    public static async Task<RegexMatchResult> GetMatchAsync(this StringMatchesAssertion assertion)
    {
        // Execute the assertion to ensure the regex has been validated and matches
        await assertion.AssertAsync();

        // Get the cached match from the assertion
        var match = assertion.GetMatch();

        if (match == null || !match.Success)
        {
            throw new InvalidOperationException("No successful match found");
        }

        return new RegexMatchResult(match);
    }
}
