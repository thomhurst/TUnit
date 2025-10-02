namespace TUnit.Assertions.AssertionBuilders.Groups;

public class AssertionGroupBuilder<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionCore
{
    private readonly TAssertionBuilder _assertionBuilder;

    internal AssertionGroupBuilder(TAssertionBuilder assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }

    public UnknownAssertionGroup<TActual, TAssertionBuilder> WithAssertion(Func<TAssertionBuilder, InvokableAssertion<TActual>> assert)
    {
        return new UnknownAssertionGroup<TActual, TAssertionBuilder>(_assertionBuilder, assert);
    }
}
