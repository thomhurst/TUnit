namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class RawReturnBehavior : IBehavior, IArgumentFreeBehavior
{
    private readonly RawReturn _wrapper;

    public RawReturnBehavior(object? rawValue) => _wrapper = new RawReturn(rawValue);

    public object? Execute(object?[] arguments) => Execute();

    public object? Execute() => _wrapper;
}

internal sealed class ComputedRawReturnBehavior : IBehavior, IArgumentFreeBehavior
{
    private readonly Func<object?> _factory;

    public ComputedRawReturnBehavior(Func<object?> factory) => _factory = factory;

    public object? Execute(object?[] arguments) => Execute();

    public object? Execute() => new RawReturn(_factory());
}

internal sealed class ComputedRawReturnWithArgsBehavior : IBehavior
{
    private readonly Func<object?[], object?> _factory;

    public ComputedRawReturnWithArgsBehavior(Func<object?[], object?> factory) => _factory = factory;

    public object? Execute(object?[] arguments) => new RawReturn(_factory(arguments));
}
