using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Assertion that maps from RegexMatchCollection to a specific RegexMatch at an index.
/// Mirrors .NET's MatchCollection[index] indexer.
/// </summary>
public class MatchIndexAssertion : Assertion<RegexMatch>
{
    private readonly int _index;

    internal MatchIndexAssertion(
        AssertionContext<RegexMatchCollection> context,
        int index)
        : base(context.Map<RegexMatch>(collection =>
        {
            if (collection == null)
            {
                throw new InvalidOperationException("No match collection available");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Match index must be >= 0");
            }

            if (index >= collection.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Match index {index} is out of range. Collection has {collection.Count} matches.");
            }

            return collection[index];
        }))
    {
        _index = index;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<RegexMatch> metadata)
    {
        var match = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            if (exception is ArgumentOutOfRangeException || exception is InvalidOperationException)
            {
                return Task.FromResult(AssertionResult.Failed(exception.Message));
            }
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // If we have a match, the assertion passed
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"match at index {_index} to exist";
}
