using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder
{
    protected IInvokableAssertionBuilder? OtherTypeAssertionBuilder;
    
    protected AssertionData? AwaitedAssertionData;

    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string actualExpression, StringBuilder expressionBuilder, Stack<BaseAssertCondition> assertions)
    {
        AssertionDataTask = assertionDataTask;
        ActualExpression = actualExpression;
        ExpressionBuilder = expressionBuilder;
        Assertions = assertions;
    }
    
    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string actualExpression)
    {
        AssertionDataTask = assertionDataTask;
        ActualExpression = actualExpression;
        
        if (string.IsNullOrEmpty(actualExpression))
        {
            ActualExpression = null;
            ExpressionBuilder = new StringBuilder("Assert.That(UNKNOWN)");
        }
        else
        {
            ActualExpression = actualExpression;
            ExpressionBuilder = new StringBuilder("Assert.That(");
            ExpressionBuilder.Append(actualExpression);
            ExpressionBuilder.Append(')');
        }
    }
    
    internal StringBuilder ExpressionBuilder { get; }
    public string? ActualExpression { get; }
    internal ValueTask<AssertionData> AssertionDataTask { get; }
    
    public Stack<BaseAssertCondition> Assertions { get; } = new();
    protected readonly List<AssertionResult> Results = [];

    protected internal AssertionBuilder AppendExpression(string expression)
    {
        if (!string.IsNullOrEmpty(expression))
        {
            ExpressionBuilder.Append('.');
            ExpressionBuilder.Append(expression);
        }

        return this;
    }

    internal AssertionBuilder AppendConnector(ChainType chainType)
    {
        if (chainType == ChainType.None)
        {
            return this;
        }
        
        return AppendExpression(chainType.ToString());
    }
    
    protected internal void AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return;
        }

        ExpressionBuilder.Append('.');
        ExpressionBuilder.Append(methodName);
        ExpressionBuilder.Append('(');

        for (var index = 0; index < expressions.Length; index++)
        {
            var expression = expressions[index];
            ExpressionBuilder.Append(expression);

            if (index < expressions.Length - 1)
            {
                ExpressionBuilder.Append(',');
                ExpressionBuilder.Append(' ');
            }
        }

        ExpressionBuilder.Append(')');
    }

    internal void WithAssertion(BaseAssertCondition assertCondition)
    {
        assertCondition = this switch
        {
            IOrAssertionBuilder => new OrAssertCondition(Assertions.Pop(), assertCondition),
            IAndAssertionBuilder => new AndAssertCondition(Assertions.Pop(), assertCondition),
            _ => assertCondition
        };

        Assertions.Push(assertCondition);
    }
    
    internal async Task<AssertionData> ProcessAssertionsAsync()
    {
        if (OtherTypeAssertionBuilder is not null)
        {
            await OtherTypeAssertionBuilder;
        }
        
        AwaitedAssertionData ??= await AssertionDataTask;

        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        foreach (var assertion in Assertions.Reverse())
        {
            var result = await assertion.Assert(AwaitedAssertionData.Value.Result, AwaitedAssertionData.Value.Exception, AwaitedAssertionData.Value.ActualExpression);
            
            Results.Add(result);
            
            if (!result.IsPassed)
            {
                if (assertion.Subject is null)
                {
                    assertion.SetSubject(AwaitedAssertionData.Value.ActualExpression);
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
        
        return AwaitedAssertionData.Value;
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