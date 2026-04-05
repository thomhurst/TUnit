namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class ReturnBehavior<TReturn> : IBehavior, IArgumentFreeBehavior
{
    private readonly TReturn _value;

    public ReturnBehavior(TReturn value) => _value = value;

    public object? Execute(object?[] arguments) => _value;

    public object? Execute() => _value;
}
