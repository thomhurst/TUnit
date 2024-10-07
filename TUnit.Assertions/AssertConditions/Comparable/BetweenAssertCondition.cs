namespace TUnit.Assertions.AssertConditions.Comparable;

public class BetweenAssertCondition<TActual>(TActual minimum, TActual maximum) : BaseAssertCondition<TActual> 
    where TActual : IComparable<TActual>
{
    private bool _inclusiveBounds;

    protected internal override string GetFailureMessage() => $"""
                                                               Expected between {minimum} & {minimum} ({GetRange()} Range)
                                                               Received: {ActualValue}
                                                               """;

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (_inclusiveBounds)
        {
            return actualValue!.CompareTo(minimum) >= 0 && actualValue.CompareTo(maximum) <= 0;
        }

        return actualValue!.CompareTo(minimum) > 0 && actualValue.CompareTo(maximum) < 0;
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