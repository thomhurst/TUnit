using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

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
    public Func<Task<AssertionData<TActual>>> AssertionDataDelegate { get; }
    
    internal readonly Stack<BaseAssertCondition<TActual>> Assertions = new();
}

public abstract class AssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actualExpression, StringBuilder? expressionBuilder, Stack<BaseAssertCondition<TActual>> assertions) : base(assertionDataDelegate, actualExpression, expressionBuilder, assertions)
    {
    }

    protected AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actual) : base(assertionDataDelegate, actual)
    {
        if (string.IsNullOrEmpty(actual))
        {
            ActualExpression = null;
            ExpressionBuilder = null;
        }
        else
        {
            ActualExpression = actual;
            ExpressionBuilder = new StringBuilder($"Assert.That({actual})");
        }
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendExpression(string expression)
    {
        ExpressionBuilder?.Append($".{expression}");
        return this;
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendConnector(ChainType chainType)
    {
        if (chainType == ChainType.None)
        {
            return this;
        }
        
        return AppendExpression(chainType.ToString());
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return this;
        }

        return AppendExpression($"{methodName}({string.Join(", ", expressions)})");
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> WithAssertion(BaseAssertCondition<TActual> assertCondition)
    {
        var builder = new InvokableAssertionBuilder<TActual, TAnd, TOr>(AssertionDataDelegate, this);

        if (this is IOrAssertionBuilder)
        {
            assertCondition = new OrAssertCondition<TActual, TAnd, TOr>(builder.Assertions.Pop(), assertCondition);
        }
        
        if (this is IAndAssertionBuilder)
        {
            assertCondition = new AndAssertCondition<TActual, TAnd, TOr>(builder.Assertions.Pop(), assertCondition);
        }
        
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