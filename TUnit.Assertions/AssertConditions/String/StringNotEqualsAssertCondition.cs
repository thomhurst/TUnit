using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsAssertCondition<TAnd, TOr> : AssertCondition<string, string>
    where TAnd : IAnd<string, TAnd, TOr>
    where TOr : IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotEqualsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        return !string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              "{ActualValue}" is equal to "{ExpectedValue}"
                                              """;
}