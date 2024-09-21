﻿using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual> ThrowsException<TActual>(this IDelegateSource<TActual> delegateSource)
    {
        return new(delegateSource.AssertionBuilder, exception => exception);
    }
    
    public static InvokableDelegateAssertionBuilder<TActual> ThrowsNothing<TActual>(this IDelegateSource<TActual> delegateSource)
    {
        return delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>()
            , []);
    }
}