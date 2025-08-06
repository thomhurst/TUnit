namespace TUnit.Core.Interfaces;

/// <summary>
/// Marker interface to indicate that a data source type should be lazily initialized.
/// When applied to types used in ClassDataSourceAttribute with SharedType.PerClass,
/// the initialization will be deferred until the first test from that class is executed,
/// rather than during test discovery.
/// </summary>
/// <remarks>
/// This interface is particularly useful for expensive-to-initialize objects like:
/// <list type="bullet">
/// <item><description>Web application factories and test servers</description></item>
/// <item><description>Database connections and containers</description></item>
/// <item><description>External service mocks or stubs</description></item>
/// <item><description>Large in-memory data structures</description></item>
/// </list>
/// 
/// When a type implements both IAsyncInitializer and IRequiresLazyInitialization,
/// the IAsyncInitializer.InitializeAsync() method will be called lazily during
/// test execution rather than eagerly during test discovery.
/// </remarks>
public interface IRequiresLazyInitialization
{
    // Marker interface - no members needed
}