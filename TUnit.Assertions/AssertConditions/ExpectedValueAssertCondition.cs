namespace TUnit.Assertions.AssertConditions;

public abstract class ExpectedValueAssertCondition<TActual, TExpected>(TExpected? expected) : BaseAssertCondition<TActual>
{
    private readonly List<(Func<TActual?, TActual?> ActualTransformation,
        Func<TExpected?, TExpected?> ExpectedTransformation)> _transformations = [];

    private readonly List<Func<TActual?, TExpected?, AssertionDecision>> _customComparers = [];

    protected TExpected? ExpectedValue { get; } = expected;

    public void WithTransform(Func<TActual?, TActual?> actualTransformation,
        Func<TExpected?, TExpected?> expectedTransformation)
    {
        _transformations.Add((actualTransformation, expectedTransformation));
    }
    
    public void WithComparer(Func<TActual?, TExpected?, AssertionDecision> comparer)
    {
        _customComparers.Add(comparer);
    }

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        var expected = ExpectedValue;
        
        foreach (var (actualTransformation, expectedTransformation) in _transformations)
        {
            actualValue = actualTransformation(actualValue);
            expected = expectedTransformation(expected);
        }
        
        foreach (var result in _customComparers.Select(customComparer => customComparer(actualValue, expected)))
        {
            switch (result)
            {
                case AssertionDecision.PassDecision:
                    return true;
                case AssertionDecision.FailDecision failDecision:
                    return FailWithMessage(failDecision.Message);
            }
        }

        return Passes(actualValue, expected);
    }
    
    protected abstract bool Passes(TActual? actualValue, TExpected? expectedValue);
    
    protected abstract string GetFailureMessage(TActual? actualValue, TExpected? expectedValue);

    protected internal override string GetFailureMessage()
    {
        return GetFailureMessage(ActualValue, ExpectedValue);
    }
}