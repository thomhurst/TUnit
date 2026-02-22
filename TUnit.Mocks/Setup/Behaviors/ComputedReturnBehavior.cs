namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class ComputedReturnBehavior<TReturn> : IBehavior
{
    private readonly Func<TReturn> _factory;

    public ComputedReturnBehavior(Func<TReturn> factory) => _factory = factory;

    public object? Execute(object?[] arguments) => _factory();
}
