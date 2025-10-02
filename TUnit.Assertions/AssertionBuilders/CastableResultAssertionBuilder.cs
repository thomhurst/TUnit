namespace TUnit.Assertions.AssertionBuilders;

public class CastableResultAssertionBuilder<TActual, TExpected> : MappableResultAssertionBuilder<TActual, TExpected>
{
    internal CastableResultAssertionBuilder(InvokableAssertion<TActual> assertionBuilder) : base(assertionBuilder, DefaultMapper)
    {
    }

    private static TExpected? DefaultMapper(TActual? actual)
    {
        try
        {
            return (TExpected?) (object?) actual;
        }
        catch
        {
            return default(TExpected?);
        }
    }
}

public class CastedAssertionBuilder<TActual, TExpected> : InvokableValueAssertion<TExpected>
{
    internal CastedAssertionBuilder(InvokableAssertion<TActual> assertionBuilder) : base(assertionBuilder)
    {
    }

    private static TExpected? DefaultMapper(TActual? actual)
    {
        try
        {
            return (TExpected?) (object?) actual;
        }
        catch
        {
            return default(TExpected?);
        }
    }
}
