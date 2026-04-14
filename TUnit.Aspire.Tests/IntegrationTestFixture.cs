namespace TUnit.Aspire.Tests;

/// <summary>
/// Aspire fixture that starts the test AppHost (including the API service)
/// with OTLP telemetry collection enabled. Shared per test session so the
/// Aspire application is started once and reused across all integration tests.
/// </summary>
public class IntegrationTestFixture : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(120);

    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllRunning;
}
