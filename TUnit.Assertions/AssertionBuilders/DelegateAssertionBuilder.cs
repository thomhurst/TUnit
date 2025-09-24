using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder : AssertionBuilder, IDelegateSource, ISource
{
    private readonly Func<object?> _actualDelegate;
    private readonly ExpressionFormatter _expressionFormatter;
    private readonly AssertionChain _chain;
    
    public DelegateAssertionBuilder(Func<object?> actualDelegate, string? actualExpression) 
        : base()
    {
        _actualDelegate = actualDelegate;
        _expressionFormatter = new ExpressionFormatter(actualExpression);
        _chain = new AssertionChain();
    }

    public override string? ActualExpression => _expressionFormatter.GetExpression();

    public override IEnumerable<BaseAssertCondition> GetAssertions() => _chain.GetAssertions();

    public override BaseAssertCondition? GetLastAssertion() => _chain.GetLastAssertion();
    
    public DelegateAssertionBuilder Or
    {
        get
        {
            _expressionFormatter.AppendConnector("Or");
            return this;
        }
    }
    
    public DelegateAssertionBuilder And
    {
        get
        {
            _expressionFormatter.AppendConnector("And");
            return this;
        }
    }
    
    IEnumerable<BaseAssertCondition> ISource.GetAssertions() => _chain.GetAssertions();
    BaseAssertCondition? ISource.GetLastAssertion() => _chain.GetLastAssertion();
    
    ValueTask<AssertionData> ISource.AssertionDataTask => GetAssertionData();
    
    StringBuilder ISource.ExpressionBuilder => new(_expressionFormatter.GetExpression());
    
    ISource ISource.AppendExpression(string expression)
    {
        _expressionFormatter.AppendConnector(expression);
        return this;
    }
    
    ISource ISource.WithAssertion(BaseAssertCondition assertCondition)
    {
        _chain.AddAssertion(assertCondition);
        return this;
    }

    public override TaskAwaiter GetAwaiter()
    {
        return ProcessAsync().GetAwaiter();
    }
    
    public override ValueTask<AssertionData> GetAssertionData()
    {
        var actualValue = _actualDelegate();
        return new ValueTask<AssertionData>(new AssertionData(
            actualValue,
            null,
            ActualExpression,
            DateTimeOffset.Now,
            DateTimeOffset.Now
        ));
    }
    
    public override async ValueTask ProcessAssertionsAsync(AssertionData data)
    {
        var evaluator = new AssertionEvaluator();
        var result = await evaluator.EvaluateAsync(new ValueTask<AssertionData>(data), _chain.GetAssertions(), _expressionFormatter);
        
        // Result handling is done by the evaluator
    }

    private async Task ProcessAsync()
    {
        var data = await GetAssertionData();
        await ProcessAssertionsAsync(data);
    }
}