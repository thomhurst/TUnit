using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition<TAnd, TOr> : AssertCondition<string, string?, TAnd, TOr>
    where TAnd : And<string?, TAnd, TOr>, IAnd<TAnd, string?, TAnd, TOr>
    where TOr : Or<string?, TAnd, TOr>, IOr<TOr, string?, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringEqualsAssertCondition(AssertionBuilder<string?> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected "{ExpectedValue}" but received "{ActualValue}"
                                              """;
}