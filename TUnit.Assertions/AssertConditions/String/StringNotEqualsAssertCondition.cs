using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
    where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotEqualsAssertCondition(AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        return !string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              "{ActualValue}" is equal to "{ExpectedValue}"
                                              """;
}