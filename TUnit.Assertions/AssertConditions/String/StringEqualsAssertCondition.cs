namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition : AssertCondition<string, string>
{
    private readonly StringComparison _stringComparison;
    private bool _trimmed;
    private bool _nullAndEmptyEquality;
    private bool _ignoreWhitespace;

    public StringEqualsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override bool Passes(string? actualValue, Exception? exception)
    {
        var actual = actualValue;
        var expected = ExpectedValue;

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

        if (_trimmed)
        {
            actual = actual?.Trim();
            expected = expected?.Trim();
        }

        if (_ignoreWhitespace)
        {
            actual = StripWhitespace(actual);
            expected = StripWhitespace(expected);
        }
        
        return string.Equals(actual, expected, _stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected: "{ExpectedValue}"
                                              Received: "{ActualValue}"
                                              {GetLocation()}
                                              """;

    private string GetLocation()
    {
        var longest = Math.Max(ActualValue?.Length ?? 0, ExpectedValue?.Length ?? 0);

        var errorIndex = -1;
        for (int i = 0; i < longest; i++)
        {
            var actualCharacter = ActualValue?.ElementAtOrDefault(i);
            var expectedCharacter = ExpectedValue?.ElementAtOrDefault(i);

            if (actualCharacter != expectedCharacter)
            {
                errorIndex = i;
                break;
            }
        }

        if (errorIndex == -1)
        {
            return string.Empty;
        }

        var startIndex = Math.Max(0, errorIndex - 10);

        var spacesPrecedingArrow = errorIndex - startIndex;
        
        return $"""
                
                
                Difference at index {errorIndex}:
                   {ActualValue?.Substring(startIndex, Math.Min(ActualValue!.Length - startIndex, 20))}
                   {new string(' ', spacesPrecedingArrow)}^
                   {ExpectedValue?.Substring(startIndex, Math.Min(ExpectedValue!.Length - startIndex, 20))}
                   {new string(' ', spacesPrecedingArrow)}^
                """;
    }

    private string? StripWhitespace(string? input)
    {
        if (input == null)
        {
            return null;
        }
        
        return string.Join(string.Empty, input.Where(c=>!char.IsWhiteSpace(c)));
    }

    public void Trimmed() => _trimmed = true;
    public void WithNullAndEmptyEquality() => _nullAndEmptyEquality = true;
    public void IgnoringWhitespace() => _ignoreWhitespace = true;
}