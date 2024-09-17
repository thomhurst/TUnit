﻿using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IValueAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IDelegateAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
{
    internal ValueDelegateAssertionBuilder(Func<TActual> function, string expressionBuilder) : base(function.AsAssertionData, expressionBuilder)
    {
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(AssertionMessageValueDelegate<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
        
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<TActual?, Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }

    public static InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>(assertionDataDelegate, assertionBuilder);
    }
}