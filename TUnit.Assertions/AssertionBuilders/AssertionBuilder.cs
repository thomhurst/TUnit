using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder<TActual>
{
    protected IInvokableAssertionBuilder? OtherTypeAssertionBuilder;
    
    protected AssertionData<TActual>? InvokedAssertionData;

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
    protected readonly List<AssertionResult> Results = [];

    protected internal AssertionBuilder<TActual> AppendExpression(string expression)
    {
        if (!string.IsNullOrEmpty(expression))
        {
            ExpressionBuilder?.Append($".{expression}");
        }

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
        var builder = new InvokableAssertionBuilder<TActual>(this);

        assertCondition = this switch
        {
            IOrAssertionBuilder => new OrAssertCondition<TActual>(builder.Assertions.Pop(), assertCondition),
            IAndAssertionBuilder => new AndAssertCondition<TActual>(builder.Assertions.Pop(), assertCondition),
            _ => assertCondition
        };

        builder.Assertions.Push(assertCondition);
        
        return builder;
    }
    
    internal async Task<AssertionData<TActual>> ProcessAssertionsAsync()
    {
        if (OtherTypeAssertionBuilder is not null)
        {
            await OtherTypeAssertionBuilder;
        }
        
        InvokedAssertionData ??= await AssertionDataDelegate();

        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        foreach (var assertion in Assertions.Reverse())
        {
            var result = await assertion.Assert(InvokedAssertionData.Result, InvokedAssertionData.Exception, InvokedAssertionData.ActualExpression);
            
            Results.Add(result);
            
            if (!result.IsPassed)
            {
                if (assertion.Subject is null)
                {
                    assertion.SetSubject(InvokedAssertionData.ActualExpression);
                }

                var exception = new AssertionException(
                    $"""
                     Expected {assertion.Subject} {assertion.GetExpectationWithReason()}
                     
                     but {result.Message}
                     
                     at {((IInvokableAssertionBuilder)this).GetExpression()}
                     """
                );
                
                if (currentAssertionScope != null)
                {
                    currentAssertionScope.AddException(exception);
                    continue;
                }
                
                throw exception;
            }
        }
        
        return InvokedAssertionData;
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