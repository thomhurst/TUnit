using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
    where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringContainsAssertCondition(AssertionBuilder<string> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
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
        
        return actualValue.Contains(ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}