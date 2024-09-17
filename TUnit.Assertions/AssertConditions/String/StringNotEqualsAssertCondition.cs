using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotEqualsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : IAnd<string, TAnd, TOr>
    where TOr : IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotEqualsAssertCondition(AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception, string? rawValueExpression)
    {
        return !string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              "{ActualValue}" is equal to "{ExpectedValue}"
                                              """;
}