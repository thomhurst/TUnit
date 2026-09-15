namespace TUnit.Core.Interfaces;

/// <summary>
/// Internal opt-in for framework fixtures whose resources belong to a test attempt.
/// Implementations deduplicate initialization within an attempt and retain ordinary
/// fixture lifetime when the per-attempt feature is not enabled.
/// </summary>
internal interface ITestAttemptInitializer : IAsyncInitializer
{
    bool IsInitialized { get; }
    ValueTask InitializeForTestAttemptAsync(TestContext context, CancellationToken cancellationToken);
}
