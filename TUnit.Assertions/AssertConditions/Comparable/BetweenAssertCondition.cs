namespace TUnit.Assertions.AssertConditions.Comparable;

public class BetweenAssertCondition<TActual>(TActual minimum, TActual maximum) : BaseAssertCondition<TActual> 
    where TActual : IComparable<TActual>
{
    private bool _inclusiveBounds;

    protected override string GetExpectation() => $"to be between {minimum} & {minimum} ({GetRange()} Range)";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        bool isInRange;

        if (_inclusiveBounds)
        {
            isInRange = actualValue!.CompareTo(minimum) >= 0 && actualValue.CompareTo(maximum) <= 0;
        }
        else
        {
            isInRange = actualValue!.CompareTo(minimum) > 0 && actualValue.CompareTo(maximum) < 0;
        }

        return AssertionResult
            .FailIf(
                () => !isInRange,
                $"received {actualValue}");

    }

    public void Inclusive()
    {
        _inclusiveBounds = true;
    }

    public void Exclusive()
    {
        _inclusiveBounds = false;
    }

    private string GetRange()
    {
        return _inclusiveBounds ? "Inclusive" : "Exclusive";
    }
}