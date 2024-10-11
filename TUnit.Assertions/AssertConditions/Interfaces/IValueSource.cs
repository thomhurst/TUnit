using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IValueSource<TActual> : ISource<TActual>
{
    InvokableValueAssertionBuilder<TActual> IsTypeOf(Type type);
    CastableAssertionBuilder<TActual, TExpected> IsTypeOf<TExpected>();

    InvokableValueAssertionBuilder<TActual> IsAssignableTo(Type type);
    CastableAssertionBuilder<TActual, TExpected> IsAssignableTo<TExpected>();

    InvokableValueAssertionBuilder<TActual> IsAssignableFrom(Type type); 
    InvokableValueAssertionBuilder<TActual> IsAssignableFrom<TExpected>();
}