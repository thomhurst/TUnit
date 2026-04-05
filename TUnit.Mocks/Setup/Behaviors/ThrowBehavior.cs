namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class ThrowBehavior : IBehavior, IArgumentFreeBehavior
{
    private readonly Exception _exception;

    public ThrowBehavior(Exception exception) => _exception = exception;

    public object? Execute(object?[] arguments) => throw _exception;

    public object? Execute() => throw _exception;
}
