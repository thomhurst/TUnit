using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder : ISource
{
    protected IInvokableAssertionBuilder? OtherTypeAssertionBuilder;
    
    protected AssertionData? AwaitedAssertionData;

    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string actualExpression, StringBuilder expressionBuilder, Stack<BaseAssertCondition> assertions)
    {
        _assertionDataTask = assertionDataTask;
        ActualExpression = actualExpression;
        _expressionBuilder = expressionBuilder;
        _assertions = assertions;
    }
    
    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string actualExpression)
    {
        _assertionDataTask = assertionDataTask;
        ActualExpression = actualExpression;
        
        if (string.IsNullOrEmpty(actualExpression))
        {
            ActualExpression = null;
            _expressionBuilder = new StringBuilder("Assert.That(UNKNOWN)");
        }
        else
        {
            ActualExpression = actualExpression;
            _expressionBuilder = new StringBuilder("Assert.That(");
            _expressionBuilder.Append(actualExpression);
            _expressionBuilder.Append(')');
        }
    }

    StringBuilder ISource.ExpressionBuilder => _expressionBuilder;

    public string? ActualExpression { get; }

    ValueTask<AssertionData> ISource.AssertionDataTask => _assertionDataTask;

    Stack<BaseAssertCondition> ISource.Assertions => _assertions;

    protected readonly List<AssertionResult> Results = [];
    private readonly StringBuilder _expressionBuilder;
    private readonly ValueTask<AssertionData> _assertionDataTask;
    private readonly Stack<BaseAssertCondition> _assertions = new();

    public ISource AppendExpression(string expression)
    {
        if (!string.IsNullOrEmpty(expression))
        {
            _expressionBuilder.Append('.');
            _expressionBuilder.Append(expression);
        }

        return this;
    }

    internal AssertionBuilder AppendConnector(ChainType chainType)
    {
        if (chainType == ChainType.None)
        {
            return this;
        }
        
        return (AssertionBuilder)AppendExpression(chainType.ToString());
    }
    
    protected internal void AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return;
        }

        _expressionBuilder.Append('.');
        _expressionBuilder.Append(methodName);
        _expressionBuilder.Append('(');

        for (var index = 0; index < expressions.Length; index++)
        {
            var expression = expressions[index];
            _expressionBuilder.Append(expression);

            if (index < expressions.Length - 1)
            {
                _expressionBuilder.Append(',');
                _expressionBuilder.Append(' ');
            }
        }

        _expressionBuilder.Append(')');
    }

    public ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        assertCondition = this switch
        {
            IOrAssertionBuilder => new OrAssertCondition(_assertions.Pop(), assertCondition),
            IAndAssertionBuilder => new AndAssertCondition(_assertions.Pop(), assertCondition),
            _ => assertCondition
        };

        _assertions.Push(assertCondition);

        return this;
    }
    
    internal async Task<AssertionData> ProcessAssertionsAsync()
    {
        if (OtherTypeAssertionBuilder is not null)
        {
            await OtherTypeAssertionBuilder;
        }
        
        AwaitedAssertionData ??= await _assertionDataTask;

        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        foreach (var assertion in _assertions.Reverse())
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