namespace TUnit.Assertions.AssertConditions.Connectors;

public class OrHas<TActual>
{
    internal BaseAssertCondition<TActual> OtherAssertCondition { get; }

    public OrHas(BaseAssertCondition<TActual> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
}