using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder 
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IOutputsChain<NoneAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>, object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IDelegateAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>
   {
    AssertionConnector<object?, DelegateAnd<object?>, DelegateOr<object?>> IAssertionBuilderProvider<object?, DelegateAnd<object?>, DelegateOr<object?>>.AssertionConnector => new(this, ChainType.Or);

    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData, expressionBuilder)
    {
    }

    public DelegateAssertionBuilder WithMessage(AssertionMessageDelegate message)
    {
        AssertionMessage = message;
        return this;
    }
                
    public DelegateAssertionBuilder WithMessage(Func<Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageDelegate) message;
        return this;
    }
    
    public DelegateAssertionBuilder WithMessage(Func<string> message)
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