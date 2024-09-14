using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotContainsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
    where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotContainsAssertCondition(AssertionBuilder<string, TAnd, TOr> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            WithMessage((_, _) => "Actual string is null");
            return false;
        }
        
        if (ExpectedValue is null)
        {
            WithMessage((_, _) => "Expected string is null");
            return false;
        }
        
        return !actualValue.Contains(ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ActualValue}" to not contain "{ExpectedValue}"
                                              """;
}