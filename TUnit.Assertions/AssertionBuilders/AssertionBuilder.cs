using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder : ISource
{
    protected IInvokableAssertionBuilder? OtherTypeAssertionBuilder;
    
    protected AssertionData? AwaitedAssertionData;

    public AssertionBuilder(ISource source)
    {
        _assertionDataTask = source.AssertionDataTask;
        _actualExpression = source.ActualExpression;
        _expressionBuilder = source.ExpressionBuilder;
        _assertions = source.Assertions;
    }
    
    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string actualExpression, StringBuilder expressionBuilder, Stack<BaseAssertCondition> assertions)
    {
        _assertionDataTask = assertionDataTask;
        _actualExpression = actualExpression;
        _expressionBuilder = expressionBuilder;
        _assertions = assertions;
    }
    
    public AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string? actualExpression)
    {
        _assertionDataTask = assertionDataTask;
        _actualExpression = actualExpression;
        
        if (string.IsNullOrEmpty(actualExpression))
        {
            _actualExpression = null;
            _expressionBuilder = new StringBuilder("Assert.That(UNKNOWN)");
        }
        else
        {
            _actualExpression = actualExpression;
            _expressionBuilder = new StringBuilder("Assert.That(");
            _expressionBuilder.Append(actualExpression);
            _expressionBuilder.Append(')');
        }
    }

    StringBuilder ISource.ExpressionBuilder => _expressionBuilder;

    string? ISource.ActualExpression => _actualExpression;

    ValueTask<AssertionData> ISource.AssertionDataTask => _assertionDataTask;

    Stack<BaseAssertCondition> ISource.Assertions => _assertions;

    protected readonly List<AssertionResult> Results = [];
    private readonly StringBuilder _expressionBuilder;
    private readonly ValueTask<AssertionData> _assertionDataTask;
    private readonly Stack<BaseAssertCondition> _assertions = new();
    private readonly string? _actualExpression;

    ISource ISource.AppendExpression(string expression)
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
        
        return (AssertionBuilder)((ISource)this).AppendExpression(chainType.ToString());
    }
    
    internal protected void AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
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

    ISource ISource.WithAssertion(BaseAssertCondition assertCondition)
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
        
        AwaitedAssertionData ??= await GetAssertionData();

        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        foreach (var assertion in _assertions.Reverse())
        {
            var result = await assertion.GetAssertionResult(AwaitedAssertionData.Value.Result, AwaitedAssertionData.Value.Exception, new AssertionMetadata
            {
                StartTime = AwaitedAssertionData.Value.Start,
                EndTime = AwaitedAssertionData.Value.End
            }, AwaitedAssertionData.Value.ActualExpression);
            
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

    private async Task<AssertionData> GetAssertionData()
    {
        var minimumWait = _assertions.Select(x => x.WaitFor).Min();
        
        if(minimumWait is null)
        {
            return await _assertionDataTask;
        }
        
        using var cts = new CancellationTokenSource();

        var completedTask = await Task.WhenAny(_assertionDataTask.AsTask(), GetMinimumWaitTask(minimumWait.Value, cts.Token));

        await cts.CancelAsync();
        
        return await completedTask;
    }

    private async Task<AssertionData> GetMinimumWaitTask(TimeSpan wait, CancellationToken token)
    {
        var start = DateTimeOffset.Now;

        await Task.Delay(wait, token);
        
        return new AssertionData(null, new CompleteWithinException($"The assertion did not complete within {wait.PrettyPrint()}"), _actualExpression, start, DateTimeOffset.Now);
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