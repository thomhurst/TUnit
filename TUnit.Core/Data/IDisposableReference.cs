namespace TUnit.Core.Data;

/// <summary>
/// Represents a disposable reference to an object managed by a scope manager.
/// </summary>
internal interface IDisposableReference
{
    /// <summary>
    /// Attempts to dispose the referenced object.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    Task DisposeAsync();
}

/// <summary>
/// A concrete implementation of a disposable reference.
/// </summary>
/// <typeparam name="T">The type of the referenced object.</typeparam>
internal class ScopedReference<T> : IDisposableReference
{
    private readonly T _instance;
    private readonly IScopeManager _scopeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedReference{T}"/> class.
    /// </summary>
    /// <param name="instance">The referenced instance.</param>
    /// <param name="scopeManager">The scope manager responsible for disposal.</param>
    public ScopedReference(T instance, IScopeManager scopeManager)
    {
        _instance = instance;
        _scopeManager = scopeManager ?? throw new ArgumentNullException(nameof(scopeManager));
    }

    /// <summary>
    /// Attempts to dispose the referenced object through its scope manager.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public async Task DisposeAsync()
    {
        await _scopeManager.TryDisposeAsync(_instance);
    }
}
