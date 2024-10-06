namespace TUnit.Assertions.AssertConditions.String;

public abstract class BaseStringValueAssertCondition(string expected, StringComparison stringComparison)
    : AssertCondition<string, string>(expected)
{
    private bool _trimmed;
    private bool _nullAndEmptyEquality;
    private bool _ignoreWhitespace;

    protected override bool Passes(string? actualValue, Exception? exception)
    {
        var actual = actualValue;
        var expected = ExpectedValue;

        if (actual == null && expected == null)
        {
            return true;
        }
        
        if (_nullAndEmptyEquality)
        {
            if (actual == null && expected == string.Empty)
            {
                return true;
            }

            if (expected == null && actualValue == string.Empty)
            {
                return true;
            }
        }

        if (actual == null || expected == null)
        {
            return false;
        }

        if (_trimmed)
        {
            actual = actual.Trim();
            expected = expected.Trim();
        }

        if (_ignoreWhitespace)
        {
            actual = StripWhitespace(actual);
            expected = StripWhitespace(expected);
        }
        
        return Passes(actual, expected, stringComparison);
    }
    
    protected abstract bool Passes(string actualValue, string expectedValue, StringComparison stringComparison);

    
    private string StripWhitespace(string input)
    {
        return string.Join(string.Empty, input.Where(c=>!char.IsWhiteSpace(c)));
    }

    public void Trimmed() => _trimmed = true;
    public void WithNullAndEmptyEquality() => _nullAndEmptyEquality = true;
    public void IgnoringWhitespace() => _ignoreWhitespace = true;
}