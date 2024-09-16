using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncDelegateAssertionBuilder 
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IOutputsChain<NoneAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>, object?, DelegateAnd<object?>, DelegateOr<object?>>, 
        IThrows<object?, DelegateAnd<object?>, DelegateOr<object?>>
{
    private readonly Func<Task> _function;

    AssertionConnector<object?, DelegateAnd<object?>, DelegateOr<object?>> IAssertionConnector<object?, DelegateAnd<object?>, DelegateOr<object?>>.AssertionConnector => new(this, ChainType.Or);

    internal AsyncDelegateAssertionBuilder(Func<Task> function, string expressionBuilder) : base(function.AsAssertionData, expressionBuilder)
    {
        _function = function;
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

    public static NoneAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> Create(Func<Task<AssertionData<object?>>> assertionDataDelegate, AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> assertionBuilder)
    {
        return new NoneAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>(assertionDataDelegate,
            assertionBuilder);
    }
}