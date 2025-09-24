using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;


namespace TUnit.Assertions.AssertionBuilders;

public class AssertionBuilder<TActual> : AssertionBuilder, IValueSource<TActual>, IDelegateSource
{
    private readonly ValueTask<AssertionData> _assertionDataTask;
    private readonly ValueTask<TActual> _actualValueTask;
    private readonly ExpressionFormatter _expressionFormatter;
    private readonly AssertionChain _chain;
    private readonly AssertionEvaluator _evaluator;
    private ChainType _currentChainType = ChainType.None;
    private string? _becauseReason;
    private string? _becauseExpression;
    
    public TActual Actual { get; }
    public override string? ActualExpression => _expressionFormatter?.GetExpression();

    // Get the last assertion added to the chain (used by wrapper classes to configure assertions)
    public override BaseAssertCondition? GetLastAssertion() => _chain.GetLastAssertion();

    public AssertionBuilder(TActual value, string? actualExpression)
        : this(new ValueTask<AssertionData>(ConvertToAssertionData(value, actualExpression)), actualExpression)
    {
        Actual = value;
        _actualValueTask = new ValueTask<TActual>(value);
    }

    public AssertionBuilder(Func<TActual> valueFunc, string? actualExpression)
        : this(EvaluateFunc(valueFunc, actualExpression), actualExpression)
    {
    }

    public AssertionBuilder(Func<Task<TActual>> asyncFunc, string? actualExpression)
        : this(EvaluateAsync(asyncFunc, actualExpression), actualExpression)
    {
    }

    public AssertionBuilder(Task<TActual> task, string? actualExpression)
        : this(EvaluateTask(task, actualExpression), actualExpression)
    {
    }

    public AssertionBuilder(ValueTask<TActual> valueTask, string? actualExpression)
        : this(EvaluateValueTask(valueTask, actualExpression), actualExpression)
    {
    }

    private AssertionBuilder(ValueTask<AssertionData> assertionDataTask, string? actualExpression)
    {
        _assertionDataTask = assertionDataTask;
        _expressionFormatter = new ExpressionFormatter(actualExpression);
        _chain = new AssertionChain();
        _evaluator = new AssertionEvaluator();
        Actual = default!; // Will be set by public constructors
        _actualValueTask = new ValueTask<TActual>(Actual);
    }

    // IValueSource implementation
    string? ISource.ActualExpression => _expressionFormatter.ActualExpression;
    ValueTask<AssertionData> ISource.AssertionDataTask => _assertionDataTask;
    IEnumerable<BaseAssertCondition> ISource.GetAssertions() => _chain.GetAssertions();
    BaseAssertCondition? ISource.GetLastAssertion() => _chain.GetLastAssertion();
    StringBuilder ISource.ExpressionBuilder => new(_expressionFormatter.GetExpression());

    ISource ISource.AppendExpression(string expression)
    {
        _expressionFormatter.AppendConnector(expression);
        return this;
    }

    ISource ISource.WithAssertion(BaseAssertCondition assertCondition)
    {
        switch (_currentChainType)
        {
            case ChainType.And:
                _chain.AddAndAssertion(assertCondition);
                break;
            case ChainType.Or:
                _chain.AddOrAssertion(assertCondition);
                break;
            default:
                _chain.AddAssertion(assertCondition);
                break;
        }
        
        _currentChainType = ChainType.None;
        return this;
    }

    public override IEnumerable<BaseAssertCondition> GetAssertions()
    {
        return _chain.GetAssertions();
    }

    public override void WithAssertion(BaseAssertCondition assertion)
    {
        _chain.AddAssertion(assertion);
    }
    
    public override void AppendExpression(string expression)
    {
        _expressionFormatter.AppendConnector(expression);
    }
    
    public ValueTask<TActual> GetActualValueTask()
    {
        return _actualValueTask;
    }

    public AssertionBuilder<TActual> And
    {
        get
        {
            _currentChainType = ChainType.And;
            _expressionFormatter.AppendConnector("And");
            return this;
        }
    }

    public AssertionBuilder<TActual> Or
    {
        get
        {
            _currentChainType = ChainType.Or;
            _expressionFormatter.AppendConnector("Or");
            return this;
        }
    }

    public override TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();

    public async Task ProcessAssertionsAsync()
    {
        var data = await GetAssertionData();
        await ProcessAssertionsAsync(data);
    }
    
    public override async ValueTask<AssertionData> GetAssertionData()
    {
        return await _assertionDataTask;
    }
    
    public override async ValueTask ProcessAssertionsAsync(AssertionData data)
    {
        await _evaluator.EvaluateAsync(new ValueTask<AssertionData>(data), _chain.GetAssertions(), _expressionFormatter);
    }

    public async Task<IEnumerable<AssertionResult>> GetAssertionResults()
    {
        await ProcessAssertionsAsync();
        return _evaluator.GetResults();
    }

    public virtual void AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (!string.IsNullOrEmpty(methodName))
        {
            _expressionFormatter.AppendMethod(methodName, expressions);
        }
    }
    
    public string? GetExpression()
    {
        return _expressionFormatter.GetExpression();
    }
    
    public AssertionBuilder<TActual> AppendConnector(ChainType chainType)
    {
        _currentChainType = chainType;
        _expressionFormatter.AppendConnector(chainType.ToString());
        return this;
    }

    
    public override void SetBecause(string reason, string? expression)
    {
        _becauseReason = reason;
        _becauseExpression = expression;
    }

    // Static helper methods
    private static AssertionData ConvertToAssertionData(TActual? value, string? actualExpression)
    {
        var start = DateTimeOffset.Now;
        return new AssertionData(value, null, actualExpression, start, DateTimeOffset.Now);
    }

    private static ValueTask<AssertionData> EvaluateFunc(Func<TActual> func, string? actualExpression)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var result = func();
            return new ValueTask<AssertionData>(new AssertionData(result, null, actualExpression, start, DateTimeOffset.Now));
        }
        catch (Exception e)
        {
            return new ValueTask<AssertionData>(new AssertionData(null, e, actualExpression, start, DateTimeOffset.Now));
        }
    }

    private static async ValueTask<AssertionData> EvaluateAsync(Func<Task<TActual>> asyncFunc, string? actualExpression)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var result = await asyncFunc();
            return new AssertionData(result, null, actualExpression, start, DateTimeOffset.Now);
        }
        catch (Exception e)
        {
            return new AssertionData(null, e, actualExpression, start, DateTimeOffset.Now);
        }
    }

    private static async ValueTask<AssertionData> EvaluateTask(Task<TActual> task, string? actualExpression)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var result = await task;
            return new AssertionData(result, null, actualExpression, start, DateTimeOffset.Now);
        }
        catch (Exception e)
        {
            return new AssertionData(null, e, actualExpression, start, DateTimeOffset.Now);
        }
    }

    private static async ValueTask<AssertionData> EvaluateValueTask(ValueTask<TActual> valueTask, string? actualExpression)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var result = await valueTask;
            return new AssertionData(result, null, actualExpression, start, DateTimeOffset.Now);
        }
        catch (Exception e)
        {
            return new AssertionData(null, e, actualExpression, start, DateTimeOffset.Now);
        }
    }


}