using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder<TActual>
{
    public AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actualExpression, StringBuilder? expressionBuilder, Stack<BaseAssertCondition<TActual>> assertions)
    {
        AssertionDataDelegate = assertionDataDelegate;
        ActualExpression = actualExpression;
        ExpressionBuilder = expressionBuilder;
        Assertions = assertions;
    }
    
    public AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actualExpression)
    {
        AssertionDataDelegate = assertionDataDelegate;
        ActualExpression = actualExpression;
        
        if (string.IsNullOrEmpty(actualExpression))
        {
            ActualExpression = null;
            ExpressionBuilder = null;
        }
        else
        {
            ActualExpression = actualExpression;
            ExpressionBuilder = new StringBuilder($"Assert.That({actualExpression})");
        }
    }
    
    internal StringBuilder? ExpressionBuilder { get; init; }
    internal string? ActualExpression { get; init; }
    internal Func<Task<AssertionData<TActual>>> AssertionDataDelegate { get; }
    
    internal readonly Stack<BaseAssertCondition<TActual>> Assertions = new();
    
    protected internal AssertionBuilder<TActual> AppendExpression(string expression)
    {
        ExpressionBuilder?.Append($".{expression}");
        return this;
    }
    
    internal AssertionBuilder<TActual> AppendConnector(ChainType chainType)
    {
        if (chainType == ChainType.None)
        {
            return this;
        }
        
        return AppendExpression(chainType.ToString());
    }
    
    protected internal AssertionBuilder<TActual> AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return this;
        }

        return AppendExpression($"{methodName}({string.Join(", ", expressions)})");
    }

    internal InvokableAssertionBuilder<TActual> WithAssertion(BaseAssertCondition<TActual> assertCondition)
    {
        var builder = new InvokableAssertionBuilder<TActual>(AssertionDataDelegate, this);

        assertCondition = this switch
        {
            IOrAssertionBuilder => new OrAssertCondition<TActual>(builder.Assertions.Pop(), assertCondition),
            IAndAssertionBuilder => new AndAssertCondition<TActual>(builder.Assertions.Pop(), assertCondition),
            _ => assertCondition
        };

        builder.Assertions.Push(assertCondition);
        
        return builder;
    }
    
    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}