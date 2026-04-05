namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class CallbackBehavior : IBehavior, IArgumentFreeBehavior
{
    private readonly Action _callback;

    public CallbackBehavior(Action callback) => _callback = callback;

    public object? Execute(object?[] arguments) => Execute();

    public object? Execute()
    {
        _callback();
        return null;
    }
}
