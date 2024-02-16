using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Connectors;

internal class AssertConditionAnd<TActual, TAnd, TOr> : BaseAssertCondition<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition1;
    private readonly BaseAssertCondition<TActual, TAnd, TOr> _condition2;

    public AssertConditionAnd(BaseAssertCondition<TActual, TAnd, TOr> condition1, BaseAssertCondition<TActual, TAnd, TOr> condition2) : base(condition1.AssertionBuilder)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);

        condition1.IsWrapped = true;
        condition2.IsWrapped = true;
        
        _condition1 = condition1;
        _condition2 = condition2;
        
        // We store assert conditions in the test context for use with Assert.Multiple
        // However, we won't be asserting them individually if we've combined them with and/or statements
        // As this handler will be registered and will control invoking them
        AssertionsTracker.Current.Remove(condition1);
        AssertionsTracker.Current.Remove(condition2);
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

            return string.Join($"{Environment.NewLine}   ", messages);
        }
    }

    protected override string DefaultMessage => string.Empty;
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception) && _condition2.Assert(actualValue, exception);
    }
}