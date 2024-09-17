using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncDelegateAssertionBuilder 
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>, 
        IDelegateAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>
{
    internal AsyncDelegateAssertionBuilder(Func<Task> function, string expressionBuilder) : base(function.AsAssertionData, expressionBuilder)
    {
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

    public static InvokableAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> Create(Func<Task<AssertionData<object?>>> assertionDataDelegate, AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> assertionBuilder)
    {
        return new InvokableAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>(assertionDataDelegate,
            assertionBuilder);
    }
}