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

    protected override AssertionResult Passes(TActual? actualValue, Exception? exception)
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
                    return AssertionResult.Passed;
                case AssertionDecision.FailDecision failDecision:
                    return FailWithMessage(failDecision.Message);
            }
        }

        return Passes(actualValue, expected);
    }
    
    protected abstract AssertionResult Passes(TActual? actualValue, TExpected? expectedValue);
}