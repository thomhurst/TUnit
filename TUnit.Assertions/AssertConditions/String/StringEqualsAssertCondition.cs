using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition<TAnd, TOr> : AssertCondition<string, string, TAnd, TOr>
    where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
    where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    
    public StringEqualsAssertCondition(AssertionBuilder<string> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected internal override bool Passes(string? actualValue, Exception? exception)
    {
        return string.Equals(actualValue, ExpectedValue, _stringComparison);
    }

    protected override string DefaultMessage => $"""
                                              Expected: "{ExpectedValue}"
                                              Received: "{ActualValue}"
                                              """;

    protected internal override string GetExtraMessage()
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
                   {new string(' ', spacesPrecedingArrow)}↑
                   {ExpectedValue?.Substring(startIndex, Math.Min(ExpectedValue!.Length - startIndex, 20))}
                   {new string(' ', spacesPrecedingArrow)}↑
                """;
    }
}