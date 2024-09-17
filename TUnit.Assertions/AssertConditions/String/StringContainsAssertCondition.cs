using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : IAnd<string, TAnd, TOr>
    where TOr : IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringContainsAssertCondition(AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (actualValue is null)
        {
            WithMessage((_, _, actualExpression) => "Actual string is null");
            return false;
        }
        
        if (ExpectedValue is null)
        {
            WithMessage((_, _, actualExpression) => "Expected string is null");
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}