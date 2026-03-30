namespace TUnit.Mocks.Setup.Behaviors;

internal sealed class VoidReturnBehavior : IBehavior
{
    public static VoidReturnBehavior Instance { get; } = new();

    public object? Execute(object?[] arguments) => null;
}
