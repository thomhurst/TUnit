namespace TUnit.Assertions.AssertionBuilders.Groups;

public class UnknownAssertionGroup<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionCore
{
    private readonly TAssertionBuilder _assertionBuilder;
    private readonly Func<TAssertionBuilder, InvokableAssertion<TActual>> _initialAssert;

    internal UnknownAssertionGroup(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, InvokableAssertion<TActual>> initialAssert)
    {
        _assertionBuilder = assertionBuilder;
        _initialAssert = initialAssert;
    }

    public AndAssertionGroup<TActual, TAssertionBuilder> And(Func<TAssertionBuilder, InvokableAssertion<TActual>> assert)
    {
        return new AndAssertionGroup<TActual, TAssertionBuilder>(_initialAssert, assert, _assertionBuilder);
    }

    public OrAssertionGroup<TActual, TAssertionBuilder> Or(Func<TAssertionBuilder, InvokableAssertion<TActual>> assert)
    {
        return new OrAssertionGroup<TActual, TAssertionBuilder>(_initialAssert, assert, _assertionBuilder);
    }
}
