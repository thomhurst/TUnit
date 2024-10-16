namespace TUnit.Assertions.AssertionBuilders.Groups;

public class UnknownAssertionGroup<TActual, TAssertionBuilder> where TAssertionBuilder : AssertionBuilder<TActual>
{
    private readonly TAssertionBuilder _assertionBuilder;
    private readonly Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> _initialAssert;

    internal UnknownAssertionGroup(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> initialAssert)
    {
        _assertionBuilder = assertionBuilder;
        _initialAssert = initialAssert;
    }

    public AndAssertionGroup<TActual, TAssertionBuilder> And(Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert)
    {
        return new AndAssertionGroup<TActual, TAssertionBuilder>(_initialAssert, assert, _assertionBuilder);
    }

    public OrAssertionGroup<TActual, TAssertionBuilder> Or(Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert)
    {
        return new OrAssertionGroup<TActual, TAssertionBuilder>(_initialAssert, assert, _assertionBuilder);
    }
}