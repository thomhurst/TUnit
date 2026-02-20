namespace TUnit.Mock.Setup.Behaviors;

internal sealed class ReturnBehavior<TReturn> : IBehavior
{
    private readonly TReturn _value;

    public ReturnBehavior(TReturn value) => _value = value;

    public object? Execute(object?[] arguments) => _value;
}
