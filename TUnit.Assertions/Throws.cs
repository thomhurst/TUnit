using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions;

public class Throws<TActual>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public WithMessage<TActual> WithMessage => new(AssertionBuilder);
    
    public ThrowsNothingAssertCondition<TActual> Nothing => new(AssertionBuilder);
    public ThrowsAnythingAssertCondition<TActual> Exception => new(AssertionBuilder);
    public ThrowsExactTypeOfAssertCondition<TActual, TExpected> TypeOf<TExpected>() => new(AssertionBuilder);
    public ThrowsSubClassOfAssertCondition<TActual, TExpected> SubClassOf<TExpected>() => new(AssertionBuilder);
}