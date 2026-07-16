namespace TUnit.Engine;

/// <summary>
/// Wraps a hook delegate with its name for activity/span creation.
/// </summary>
internal readonly record struct NamedHookDelegate<TContext>(string Name, string ActivityName, Func<TContext, CancellationToken, Task> Invoke)
{
    public NamedHookDelegate(string name, Func<TContext, CancellationToken, Task> invoke)
        : this(name, $"hook: {name}", invoke)
    {
    }
}
