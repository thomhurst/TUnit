namespace TUnit.Assertions.AssertionBuilders;

public class CastableResultAssertionBuilder<TActual, TExpected> : MappableResultAssertionBuilder<TActual, TExpected>
{
    internal CastableResultAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, DefaultMapper)
    {
    }

    private static TExpected? DefaultMapper(TActual? actual)
    {
        try
        {
            return (TExpected?)(object?)actual;
        }
        catch
        {
            return default;
        }
    }
}

public class CastedAssertionBuilder<TActual, TExpected> : InvokableValueAssertionBuilder<TExpected>
{
    internal CastedAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {
    }

    private static TExpected? DefaultMapper(TActual? actual)
    {
        try
        {
            return (TExpected?)(object?)actual;
        }
        catch
        {
            return default;
        }
    }
}