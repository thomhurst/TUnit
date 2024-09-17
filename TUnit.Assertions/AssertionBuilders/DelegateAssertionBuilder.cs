using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>,
        IDelegateSource<object?, DelegateAnd<object?>, DelegateOr<object?>>
{
    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    public DelegateAssertionBuilder WithMessage(AssertionMessageDelegate message)
    {
        AssertionMessage = message;
        return this;
    }

    public DelegateAssertionBuilder WithMessage(Func<Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageDelegate)message;
        return this;
    }

    public DelegateAssertionBuilder WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageDelegate)message;
        return this;
    }

    public static InvokableAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> Create(
        Func<Task<AssertionData<object?>>> assertionDataDelegate,
        AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> assertionBuilder)
    {
        return new InvokableAssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>>(assertionDataDelegate,
            assertionBuilder);
    }

    AssertionBuilder<object?, DelegateAnd<object?>, DelegateOr<object?>> ISource<object?, DelegateAnd<object?>, DelegateOr<object?>>.AssertionBuilder => this;
}