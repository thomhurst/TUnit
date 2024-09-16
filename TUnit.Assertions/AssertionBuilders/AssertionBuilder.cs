using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder<TActual>
{
    public AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate)
    {
        AssertionDataDelegate = assertionDataDelegate;
    }
    
    internal StringBuilder? ExpressionBuilder { get; init; }
    internal string? RawActualExpression { get; init; }
    public AssertionMessage? AssertionMessage { get; protected set; }
    
    public Func<Task<AssertionData<TActual>>> AssertionDataDelegate { get; }
    
    internal readonly List<BaseAssertCondition<TActual>> Assertions = new();
}

public abstract class AssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate) : base(assertionDataDelegate)
    {
    }

    protected AssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actual) : base(assertionDataDelegate)
    {
        if (string.IsNullOrEmpty(actual))
        {
            RawActualExpression = null;
            ExpressionBuilder = null;
        }
        else
        {
            RawActualExpression = actual;
            ExpressionBuilder = new StringBuilder($"AssertAsync.That({actual})");
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

        if (ExpressionBuilder?.ToString().EndsWith($".{chainType}") == true)
        {
            return this;
        }
        
        return AppendExpression(chainType.ToString());
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethod(string? expression, [CallerMemberName] string methodName = "")
    {
        return AppendCallerMethodWithMultipleExpressions([expression], methodName);
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethodWithMultipleExpressions(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return this;
        }

        return AppendExpression($"{methodName}({string.Join(", ", expressions)})");
    }

    public TOutput WithAssertion<TAssertionBuilder, TOutput>(BaseAssertCondition<TActual> assertCondition)
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        var builder = TAssertionBuilder.Create(AssertionDataDelegate, this);
        builder.Assertions.Add(assertCondition);
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