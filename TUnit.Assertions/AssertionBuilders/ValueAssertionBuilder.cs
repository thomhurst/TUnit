using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> 
    : AssertionBuilder<TActual>,
        IValueSource<TActual>
{
    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => this;
    
    public InvokableValueAssertionBuilder<TActual> IsTypeOf(Type type)
    {
        return new ValueSource<TActual>(this).IsTypeOf(type);
    }

    public CastableAssertionBuilder<TActual, TExpected> IsTypeOf<TExpected>()
    {
        return new ValueSource<TActual>(this).IsTypeOf<TExpected>();
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableTo(Type type)
    {
        return new ValueSource<TActual>(this).IsAssignableTo(type);
    }

    public CastableAssertionBuilder<TActual, TExpected> IsAssignableTo<TExpected>()
    {
        return new ValueSource<TActual>(this).IsAssignableTo<TExpected>();
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom(Type type)
    {
        return new ValueSource<TActual>(this).IsAssignableFrom(type);
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom<TExpected>()
    {
        return new ValueSource<TActual>(this).IsAssignableFrom<TExpected>();
    }
    
    public InvokableValueAssertionBuilder<TActual> IsNotTypeOf(Type type)
    {
        return new ValueSource<TActual>(this).IsNotTypeOf(type);
    }

    public InvokableValueAssertionBuilder<TActual> IsNotTypeOf<TExpected>()
    {
        return new ValueSource<TActual>(this).IsNotTypeOf<TExpected>();
    }

    public InvokableValueAssertionBuilder<TActual> IsNotAssignableTo(Type type)
    {
        return new ValueSource<TActual>(this).IsNotAssignableTo(type);
    }

    public InvokableValueAssertionBuilder<TActual> IsNotAssignableTo<TExpected>()
    {
        return new ValueSource<TActual>(this).IsNotAssignableTo<TExpected>();
    }

    public InvokableValueAssertionBuilder<TActual> IsNotAssignableFrom(Type type)
    {
        return new ValueSource<TActual>(this).IsNotAssignableFrom(type);
    }

    public InvokableValueAssertionBuilder<TActual> IsNotAssignableFrom<TExpected>()
    {
        return new ValueSource<TActual>(this).IsNotAssignableFrom<TExpected>();
    }
}