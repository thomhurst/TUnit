﻿using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> : AssertionBuilder, IValueSource<TActual>
{
    internal ValueAssertionBuilder(TActual value, string? expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    public ValueAssertionBuilder(ISource source) : base(source)
    {
    }

    public new ISource AppendExpression(string expression)
    {
        base.AppendExpression(expression);
        return this;
    }
    
    public new ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        base.WithAssertion(assertCondition);
        return this;
    }
}