using TUnit.Core;

namespace TUnit.Engine.Framework;

/// <summary>
/// Provides access to services related to test execution and lifecycle management.
/// </summary>
internal interface ITestExecutionServices
{
    TestExecutor TestExecutor { get; }
    TestSessionCoordinator TestSessionCoordinator { get; }
    EngineCancellationToken CancellationToken { get; }
    CancellationTokenSource FailFastCancellationSource { get; }
    bool AfterSessionHooksFailed { get; set; }
}
