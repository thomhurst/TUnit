namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test-specific events for advanced integration scenarios.
/// Accessed via <see cref="TestContext.Events"/>.
/// </summary>
public interface ITestEvents
{
    /// <summary>
    /// Gets the event manager for this test, providing hooks for custom test lifecycle integration.
    /// </summary>
    TestContextEvents Events { get; }
}
