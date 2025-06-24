namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for types that require asynchronous initialization before they can be used.
/// </summary>
/// <remarks>
/// Implementations of this interface are automatically initialized by the TUnit framework when
/// they are injected into test classes or used as data sources. The initialization occurs before
/// any test execution that depends on the instance.
/// </remarks>
public interface IAsyncInitializer
{
    /// <summary>
    /// Asynchronously initializes the instance.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous initialization operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Called by the TUnit framework to initialize the instance before it is used
    /// in test execution. The framework guarantees that this method will be called exactly once
    /// per instance lifecycle.
    /// </para>
    /// <para>
    /// Use this method to perform setup operations such as:
    /// <list type="bullet">
    /// <item><description>Connecting to databases or external services</description></item>
    /// <item><description>Starting in-memory servers or test containers</description></item>
    /// <item><description>Loading test data</description></item>
    /// <item><description>Preparing the object's internal state</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For cleanup operations, consider implementing <see cref="IAsyncDisposable"/> alongside this interface.
    /// </para>
    /// </remarks>
    Task InitializeAsync();
}