namespace TUnit.Assertions.AssertConditions.Throws;

public class WithMessage<TActual>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }

    public WithMessage(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<TActual, string> EqualTo(string expected)
    {
        return EqualTo(expected, StringComparison.Ordinal);
    }
    
    public AssertCondition<TActual, string> EqualTo(string expected, StringComparison stringComparison)
    {
        return new ThrowsWithMessageEqualToAssertCondition<TActual>(AssertionBuilder, expected, stringComparison);
    }
    
    public AssertCondition<TActual, string> Containing(string expected)
    {
        return Containing(expected, StringComparison.Ordinal);
    }
    
    public AssertCondition<TActual, string> Containing(string expected, StringComparison stringComparison)
    {
        return new ThrowsWithMessageContainingAssertCondition<TActual>(AssertionBuilder, expected, stringComparison);
    }
}