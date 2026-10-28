using System.Collections.Concurrent;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7271: DCP-backed resources log
/// under runtime instance names (model name plus a unique suffix), so log reads and subscriptions
/// keyed by the model name saw none of a crashed executable's console output. These run a real
/// executable that exits non-zero during startup, rather than an in-memory logger registered under
/// the model name (which does not reproduce the bug).
/// </summary>
[Category("Integration")]
public class CrashedExecutableLogTests
{
    // `dotnet <unknown-option>` exits 1 and echoes the option back in its error, so the marker
    // proves the line came from the crashed process's console stream.
    private const string ProbeMarker = "nonexistent-startup-probe";

    [Test]
    public async Task StartupDiagnostics_IncludeCrashedExecutableOutput()
    {
        var fixture = new CrashingProbeFixture(retain: false);
        try
        {
            await Assert.That(fixture.InitializeAsync).Throws<InvalidOperationException>();

            // The startup-failure dump reads logs live (no retention), via the persisted console stream.
            var dump = fixture.ProgressMessages.FirstOrDefault(m => m.Contains("resource diagnostics"));
            await Assert.That(dump).IsNotNull();
            await Assert.That(dump!).Contains(ProbeMarker);
            await Assert.That(dump).DoesNotContain("(no logs available)");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Test]
    public async Task RetainedLogs_FollowRuntimeInstance_AndSurviveDisposal()
    {
        var fixture = new CrashingProbeFixture(retain: true);
        try
        {
            await Assert.That(fixture.InitializeAsync).Throws<InvalidOperationException>();
        }
        finally
        {
            await fixture.DisposeAsync();
        }

        var logs = await fixture.GetResourceLogsAsync(CrashingProbeFixture.ProbeName);

        await Assert.That(logs).IsNotEmpty();
        await Assert.That(logs.Any(line => line.Contains(ProbeMarker))).IsTrue();
    }

    public sealed class CrashingProbeFixture(bool retain) : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
    {
        public const string ProbeName = "probe";

        public ConcurrentQueue<string> ProgressMessages { get; } = new();

        protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(60);

        protected override bool EnableTelemetryCollection => false;

        protected override AspireFixtureOptions Options { get; } = new()
        {
            ForwardResourceLogs = true,
            RetainResourceLogs = retain,
            ResourceLogNames = [ProbeName],
        };

        // Only the crashing executable: no Docker container and no project build needed.
        protected override IEnumerable<string> ResourcesToRemove() => ["nginx-no-healthcheck", "api-service"];

        protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
        {
            // Never healthy, so the fixture keeps waiting until it observes the non-zero exit
            // instead of treating the briefly-Running process as ready.
            builder.Services.AddHealthChecks().AddCheck("probe-never-healthy", () => HealthCheckResult.Unhealthy());

            builder.AddExecutable(ProbeName, "dotnet", Environment.CurrentDirectory, "--" + ProbeMarker)
                .WithHealthCheck("probe-never-healthy");
        }

        protected override void LogProgress(string message)
        {
            ProgressMessages.Enqueue(message);
            base.LogProgress(message);
        }
    }
}
