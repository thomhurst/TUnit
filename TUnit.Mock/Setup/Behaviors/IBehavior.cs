using System.ComponentModel;

namespace TUnit.Mock.Setup.Behaviors;

/// <summary>
/// Represents a behavior to execute when a mocked method is called.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IBehavior
{
    object? Execute(object?[] arguments);
}
