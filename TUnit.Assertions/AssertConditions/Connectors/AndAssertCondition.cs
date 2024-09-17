using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Connectors;

internal class AndAssertCondition<TActual, TAnd, TOr> : BaseAssertCondition<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public AndAssertCondition(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string Message
    {
        get
        {
            var messages = new List<string>(2);
            
            if (!_condition1.Assert(ActualValue, Exception, RawActualExpression))
            {
                messages.Add(_condition1.Message);
            }
            
            if (!_condition2.Assert(ActualValue, Exception, RawActualExpression))
            {
                messages.Add(_condition2.Message);
            }

            return string.Join($"{Environment.NewLine} and{Environment.NewLine}", messages);
        }
    }

    protected override string DefaultMessage => string.Empty;
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return _condition1.Assert(actualValue, exception, rawValueExpression) && _condition2.Assert(actualValue, exception, rawValueExpression);
    }
}