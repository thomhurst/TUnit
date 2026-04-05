using System.ComponentModel;

namespace TUnit.Mocks.Setup.Behaviors;

/// <summary>
/// Behavior that computes a return value from the method arguments.
/// Public for generated code access. Not intended for direct use.
/// </summary>
/// <remarks>
/// Future optimization: implement ITypedBehavior&lt;T...&gt; to avoid store.ToArray() when args are needed.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ComputedReturnWithArgsBehavior<TReturn> : IBehavior
{
    private readonly Func<object?[], TReturn> _factory;

    public ComputedReturnWithArgsBehavior(Func<object?[], TReturn> factory)
    {
        _factory = factory;
    }

    public object? Execute(object?[] arguments)
    {
        return _factory(arguments);
    }
}
