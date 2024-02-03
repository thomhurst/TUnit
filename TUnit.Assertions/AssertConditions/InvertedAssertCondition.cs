namespace TUnit.Assertions.AssertConditions;

public class InvertedAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    private readonly AssertCondition<TActual, TExpected> _conditionToInvert;

    public InvertedAssertCondition(AssertCondition<TActual, TExpected> conditionToInvert,
        Func<TActual?, Exception?, string> messageFactory) : base(conditionToInvert.AssertionBuilder, conditionToInvert.ExpectedValue)
    {
        _conditionToInvert = conditionToInvert;
        WithMessage(messageFactory);
    }

    protected override string DefaultMessage => string.Empty;
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return !_conditionToInvert.Passes(actualValue, exception);
    }
}