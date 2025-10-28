using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Assertion for a specific regex match with lambda support.
/// </summary>
public class MatchAssertion : Assertion<RegexMatchCollection>
{
    private readonly int _index;
    private readonly Func<ValueAssertion<RegexMatch>, Assertion<RegexMatch>?> _matchAssertion;

    internal MatchAssertion(
        AssertionContext<RegexMatchCollection> context,
        int index,
        Func<ValueAssertion<RegexMatch>, Assertion<RegexMatch>?> assertion)
        : base(context)
    {
        _index = index;
        _matchAssertion = assertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<RegexMatchCollection> metadata)
    {
        var collection = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}");
        }

        if (collection == null)
        {
            return AssertionResult.Failed("No match collection available");
        }

        if (_index < 0 || _index >= collection.Count)
        {
            return AssertionResult.Failed(
                $"Match index {_index} is out of range. Collection has {collection.Count} matches.");
        }

        RegexMatch match;
        try
        {
            match = collection[_index];
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"Failed to get match at index {_index}: {ex.Message}");
        }

        var matchAssertion = new ValueAssertion<RegexMatch>(match, $"match at index {_index}");
        var resultingAssertion = _matchAssertion(matchAssertion);
        if (resultingAssertion != null)
        {
            await resultingAssertion.AssertAsync();
        }
        return AssertionResult.Passed;
    }

    protected override string GetExpectation()
    {
        return $"match at index {_index} to satisfy assertion";
    }
}
