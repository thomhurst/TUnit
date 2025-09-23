namespace TUnit.Assertions.AssertionBuilders.Groups;

public class AssertionGroupBuilder<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionBuilder
{
    private readonly TAssertionBuilder _assertionBuilder;

    internal AssertionGroupBuilder(TAssertionBuilder assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }

    public UnknownAssertionGroup<TActual, TAssertionBuilder> WithAssertion(Func<TAssertionBuilder, AssertionBuilder<TActual>> assert)
    {
        return new UnknownAssertionGroup<TActual, TAssertionBuilder>(_assertionBuilder, assert);
    }
}
