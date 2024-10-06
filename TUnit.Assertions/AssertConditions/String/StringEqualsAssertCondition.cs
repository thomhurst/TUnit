namespace TUnit.Assertions.AssertConditions.String;

public class StringEqualsAssertCondition(string expected, StringComparison stringComparison)
    : BaseStringValueAssertCondition(expected, stringComparison)
{
    protected override bool Passes(string actualValue, string expectedValue, StringComparison stringComparison)
    {
        return string.Equals(actualValue, expectedValue, stringComparison);
    }
    
    protected internal override string GetFailureMessage() => $"""
                                                               Expected: "{ExpectedValue}"
                                                               Received: "{ActualValue}"
                                                               {GetLocation()}
                                                               """;

    private string GetLocation()
    {
        var longest = Math.Max(ActualValue?.Length ?? 0, ExpectedValue?.Length ?? 0);

        var errorIndex = -1;
        for (var i = 0; i < longest; i++)
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
                   {new string(' ', spacesPrecedingArrow)}^
                   {ExpectedValue?.Substring(startIndex, Math.Min(ExpectedValue!.Length - startIndex, 20))}
                   {new string(' ', spacesPrecedingArrow)}^
                """;
    }
}