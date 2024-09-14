﻿using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder 
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IThrows<object?, DelegateAnd<object?>, DelegateOr<object?>>
   {
    private readonly Action _action;

    Throws<object?, DelegateAnd<object?>, DelegateOr<object?>> IThrows<object?, DelegateAnd<object?>, DelegateOr<object?>>.Throws() => new(this, ConnectorType.None, null);

    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(expressionBuilder)
    {
        _action = action;
    }

    protected internal override Task<AssertionData<object?>> GetAssertionData()
    {
        var exception = _action.InvokeAndGetException();
        
        return Task.FromResult(new AssertionData<object?>(null, exception));
    }

    public DelegateAssertionBuilder WithMessage(AssertionMessageDelegate message)
    {
        AssertionMessage = message;
        return this;
    }
                
    public DelegateAssertionBuilder WithMessage(Func<Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageDelegate) message;
        return this;
    }
    
    public DelegateAssertionBuilder WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageDelegate) message;
        return this;
    }
}