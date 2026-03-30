namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class RawReturnBehavior : IBehavior
{
    private readonly RawReturn _wrapper;

    public RawReturnBehavior(object? rawValue) => _wrapper = new RawReturn(rawValue);

    public object? Execute(object?[] arguments) => _wrapper;
}

internal sealed class ComputedRawReturnBehavior : IBehavior
{
    private readonly Func<object?> _factory;

    public ComputedRawReturnBehavior(Func<object?> factory) => _factory = factory;

    public object? Execute(object?[] arguments) => new RawReturn(_factory());
}
