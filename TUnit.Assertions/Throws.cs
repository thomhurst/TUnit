using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions;

public class Throws<TActual>
{
    internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    public ThrowsNothingAssertCondition<TActual> Nothing => new(AssertionBuilder);
}