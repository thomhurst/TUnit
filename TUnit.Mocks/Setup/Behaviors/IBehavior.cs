using System.ComponentModel;

namespace TUnit.Mocks.Setup.Behaviors;

/// <summary>
/// Represents a behavior to execute when a mocked method is called.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IBehavior
{
    object? Execute(object?[] arguments);
}

/// <summary>
/// Marker interface for behaviors that do not use the method arguments.
/// Implementing this avoids the allocation of the boxed argument array on the invocation hot path.
/// Custom <see cref="IBehavior"/> implementations that ignore arguments can implement this
/// to participate in the fast path.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IArgumentFreeBehavior
{
    object? Execute();
}
