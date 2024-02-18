using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncDelegateAssertionBuilder : AssertionBuilder<object?>
{
    private readonly Func<Task> _function;
    
    public Throws<object?, DelegateAnd<object?>, DelegateOr<object?>> Throws => new(this, ConnectorType.None, null);

    internal AsyncDelegateAssertionBuilder(Func<Task> function, string expressionBuilder) : base(expressionBuilder)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<object?>> GetAssertionData()
    {
        var exception = await _function.InvokeAndGetExceptionAsync();
        
        return new AssertionData<object?>(null, exception);
    }
    
    public AsyncDelegateAssertionBuilder WithMessage(AssertionMessageDelegate message)
    {
        AssertionMessage = message;
        return this;
    }
                    
    public AsyncDelegateAssertionBuilder WithMessage(Func<Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageDelegate) message;
        return this;
    }
    
    public AsyncDelegateAssertionBuilder WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageDelegate) message;
        return this;
    }
}