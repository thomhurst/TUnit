﻿using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.String;

public class StringNotContainsAssertCondition<TAnd, TOr> : AssertCondition<string, string>
    where TAnd : IAnd<string, TAnd, TOr>
    where TOr : IOr<string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringNotContainsAssertCondition(string expected, StringComparison stringComparison) : base(expected)
    {
        _stringComparison = stringComparison;
    }
    
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = "Actual string is null";
            return false;
        }
        
        if (ExpectedValue is null)
        {
            OverriddenMessage = "Expected string is null";
            return false;
        }
        
        return !actualValue.Contains(ExpectedValue, _stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected "{ActualValue}" to not contain "{ExpectedValue}"
                                              """;
}