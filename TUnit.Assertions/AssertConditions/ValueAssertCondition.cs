namespace TUnit.Assertions.AssertConditions;

public abstract class ValueAssertCondition<TActual>
    : BaseAssertCondition<TActual>
{
    private readonly List<Func<TActual?, TActual?>> _transformations = [];

    private readonly List<Func<TActual?, AssertionDecision>> _customComparers = [];


    public void WithTransform(Func<TActual?, TActual?> actualTransformation)
    {
        _transformations.Add(actualTransformation);
    }
    
    public void WithComparer(Func<TActual?, AssertionDecision> comparer)
    {
        _customComparers.Add(comparer);
    }

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        if (exception is not null)
        {
            return FailWithMessage($"A {exception.GetType().Name} was thrown.");
        }

        foreach (var actualTransformation in _transformations)
        {
            actualValue = actualTransformation(actualValue);
        }
        
        foreach (var result in _customComparers.Select(customComparer => customComparer(actualValue)))
        {
            switch (result)
            {
                case AssertionDecision.PassDecision:
                    return AssertionResult.Passed;
                case AssertionDecision.FailDecision failDecision:
                    return FailWithMessage(failDecision.Message);
            }
        }

        return Passes(actualValue);
    }
    
    protected abstract AssertionResult Passes(TActual? actualValue);
    
    protected abstract string GetFailureMessage(TActual? actualValue);

    protected override string GetExpectation()
    {
        return GetFailureMessage(ActualValue);
    }
}