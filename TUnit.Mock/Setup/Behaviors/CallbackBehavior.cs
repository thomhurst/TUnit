namespace TUnit.Mock.Setup.Behaviors;

internal sealed class CallbackBehavior : IBehavior
{
    private readonly Action _callback;

    public CallbackBehavior(Action callback) => _callback = callback;

    public object? Execute(object?[] arguments)
    {
        _callback();
        return null;
    }
}
