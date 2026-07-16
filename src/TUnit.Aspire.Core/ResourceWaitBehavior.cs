namespace TUnit.Aspire;

/// <summary>
/// Specifies how <see cref="AspireFixture{TAppHost}"/> should wait for resources during initialization.
/// </summary>
public enum ResourceWaitBehavior
{
    /// <summary>
    /// Wait for all resources to pass health checks (default).
    /// </summary>
    AllHealthy,

    /// <summary>
    /// Wait for all resources to reach the Running state.
    /// </summary>
    AllRunning,

    /// <summary>
    /// Wait only for resources returned by <see cref="AspireFixture{TAppHost}.ResourcesToWaitFor"/>.
    /// </summary>
    Named,

    /// <summary>
    /// Don't wait for any resources - the user handles readiness manually.
    /// </summary>
    None
}
