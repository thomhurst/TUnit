using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Assertion for a regex capture group on a match collection (operates on first match).
/// </summary>
public class GroupAssertion : Assertion<RegexMatchCollection>
{
    private readonly Func<ValueAssertion<string>, Assertion<string>?> _groupAssertion;
    private readonly string? _groupName;
    private readonly int? _groupIndex;

    internal GroupAssertion(
        AssertionContext<RegexMatchCollection> context,
        string groupName,
        Func<ValueAssertion<string>, Assertion<string>?> assertion,
        bool _)
        : base(context)
    {
        _groupName = groupName;
        _groupAssertion = assertion;
    }

    internal GroupAssertion(
        AssertionContext<RegexMatchCollection> context,
        int groupIndex,
        Func<ValueAssertion<string>, Assertion<string>?> assertion,
        bool _)
        : base(context)
    {
        _groupIndex = groupIndex;
        _groupAssertion = assertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<RegexMatchCollection> metadata)
    {
        var collection = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}");
        }

        if (collection == null || collection.Count == 0)
        {
            return AssertionResult.Failed("No match results available");
        }

        string groupValue;
        try
        {
            groupValue = _groupName != null
                ? collection.First.GetGroup(_groupName)
                : collection.First.GetGroup(_groupIndex!.Value);
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed(ex.Message);
        }

        var groupAssertion = new ValueAssertion<string>(groupValue, _groupName ?? $"group {_groupIndex}");
        var resultingAssertion = _groupAssertion(groupAssertion);
        if (resultingAssertion != null)
        {
            await resultingAssertion.AssertAsync();
        }
        return AssertionResult.Passed;
    }

    protected override string GetExpectation()
    {
        if (_groupName != null)
        {
            return $"group '{_groupName}' to satisfy assertion";
        }
        return $"group {_groupIndex} to satisfy assertion";
    }
}

/// <summary>
/// Assertion for a regex capture group on a specific match.
/// </summary>
public class MatchGroupAssertion : Assertion<RegexMatch>
{
    private readonly Func<ValueAssertion<string>, Assertion<string>?> _groupAssertion;
    private readonly string? _groupName;
    private readonly int? _groupIndex;

    internal MatchGroupAssertion(
        AssertionContext<RegexMatch> context,
        string groupName,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
        : base(context)
    {
        _groupName = groupName;
        _groupAssertion = assertion;
    }

    internal MatchGroupAssertion(
        AssertionContext<RegexMatch> context,
        int groupIndex,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
        : base(context)
    {
        _groupIndex = groupIndex;
        _groupAssertion = assertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<RegexMatch> metadata)
    {
        var match = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}");
        }

        if (match == null)
        {
            return AssertionResult.Failed("No match available");
        }

        string groupValue;
        try
        {
            groupValue = _groupName != null
                ? match.GetGroup(_groupName)
                : match.GetGroup(_groupIndex!.Value);
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed(ex.Message);
        }

        var groupAssertion = new ValueAssertion<string>(groupValue, _groupName ?? $"group {_groupIndex}");
        var resultingAssertion = _groupAssertion(groupAssertion);
        if (resultingAssertion != null)
        {
            await resultingAssertion.AssertAsync();
        }
        return AssertionResult.Passed;
    }

    protected override string GetExpectation()
    {
        if (_groupName != null)
        {
            return $"group '{_groupName}' to satisfy assertion";
        }
        return $"group {_groupIndex} to satisfy assertion";
    }
}
