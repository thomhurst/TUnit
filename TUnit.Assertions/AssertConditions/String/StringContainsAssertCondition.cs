namespace TUnit.Assertions.AssertConditions.String;

public class StringContainsAssertCondition(string expected, StringComparison stringComparison)
    : BaseStringValueAssertCondition(expected, stringComparison)
{
    protected override bool Passes(string actualValue, string expectedValue, StringComparison stringComparison)
    {
        return actualValue.Contains(expectedValue, stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                                               Expected "{ActualValue}" to contain "{ExpectedValue}"
                                                               """;
}