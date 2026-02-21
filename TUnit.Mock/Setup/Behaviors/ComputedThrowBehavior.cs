using System.ComponentModel;

namespace TUnit.Mock.Setup.Behaviors;

/// <summary>
/// Behavior that computes an exception from the method arguments and throws it.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ComputedThrowBehavior : IBehavior
{
    private readonly Func<object?[], Exception> _exceptionFactory;

    public ComputedThrowBehavior(Func<object?[], Exception> exceptionFactory)
    {
        _exceptionFactory = exceptionFactory;
    }

    public object? Execute(object?[] arguments)
    {
        throw _exceptionFactory(arguments);
    }
}
