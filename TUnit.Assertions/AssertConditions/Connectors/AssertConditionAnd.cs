﻿using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Connectors;

internal class AssertConditionAnd<TActual, TAnd, TOr> : BaseAssertCondition<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition1;
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition2;

    public AssertConditionAnd(BaseAssertCondition<TActual, TAnd, TOr> condition1, BaseAssertCondition<TActual, TAnd, TOr> condition2) : base(condition1.AssertionBuilder)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);

        condition1.IsWrapped = true;
        condition2.IsWrapped = true;
        IsWrapped = true;
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string Message
    {
        get
        {
            var messages = new List<string>(2);
            
            if (!_condition1.Assert(ActualValue, Exception))
            {
                messages.Add(_condition1.Message);
            }
            
            if (!_condition2.Assert(ActualValue, Exception))
            {
                messages.Add(_condition2.Message);
            }

            return string.Join($"{Environment.NewLine}", messages);
        }
    }

    protected override string DefaultMessage => string.Empty;
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception) && _condition2.Assert(actualValue, exception);
    }
}