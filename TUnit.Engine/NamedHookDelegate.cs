namespace TUnit.Engine;

/// <summary>
/// Wraps a hook delegate with its name for activity/span creation.
/// </summary>
internal readonly record struct NamedHookDelegate<TContext>(string Name, Func<TContext, CancellationToken, Task> Invoke);
