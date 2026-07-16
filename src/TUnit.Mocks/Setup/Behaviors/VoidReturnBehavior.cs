namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class VoidReturnBehavior : IBehavior, IArgumentFreeBehavior
{
    public static VoidReturnBehavior Instance { get; } = new();

    public object? Execute(object?[] arguments) => Execute();

    public object? Execute() => null;
}
