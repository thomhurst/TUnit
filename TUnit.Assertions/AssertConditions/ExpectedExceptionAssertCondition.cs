namespace TUnit.Assertions.AssertConditions;

public abstract class ExpectedExceptionAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly List<Func<Exception?, AssertionDecision>> _customComparers = [];
    
    public void WithComparer(Func<Exception?, AssertionDecision> comparer)
    {
        _customComparers.Add(comparer);
    }

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        foreach (var result in _customComparers.Select(customComparer => customComparer(exception)))
        {
            switch (result)
            {
                case AssertionDecision.PassDecision:
                    return true;
                case AssertionDecision.FailDecision failDecision:
                    return FailWithMessage(failDecision.Message);
            }
        }

        return Passes(exception);
    }
    
    protected abstract bool Passes(Exception? expectedValue);
}