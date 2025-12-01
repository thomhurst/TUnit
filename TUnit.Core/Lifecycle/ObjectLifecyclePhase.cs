namespace TUnit.Core.Lifecycle;

/// <summary>
/// Represents the lifecycle phases of an object managed by TUnit.
/// Objects progress through these phases during test execution.
/// </summary>
public enum ObjectLifecyclePhase
{
    /// <summary>
    /// Object has not been tracked by the lifecycle manager yet.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Object has been discovered during test building/registration.
    /// Property values have been generated but not yet injected.
    /// </summary>
    Registered = 1,

    /// <summary>
    /// Property injection has been completed on this object.
    /// Any properties with data source attributes have been populated.
    /// </summary>
    PropertiesInjected = 2,

    /// <summary>
    /// IAsyncInitializer.InitializeAsync() has been called and completed.
    /// The object is fully initialized and ready for use.
    /// </summary>
    Initialized = 3,

    /// <summary>
    /// Object is currently being used by one or more tests.
    /// Reference count is > 0.
    /// </summary>
    Active = 4,

    /// <summary>
    /// Object disposal has begun.
    /// IAsyncDisposable.DisposeAsync() or IDisposable.Dispose() is being called.
    /// </summary>
    Disposing = 5,

    /// <summary>
    /// Object has been fully disposed and cleaned up.
    /// </summary>
    Disposed = 6
}
