using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static TUnit.Aspire.AspireFixture<object>;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Unit tests for the Aspire startup-diagnosis helpers (no Docker required). These cover the pure
/// classification, hint, description, and timeline-formatting logic that turns an opaque startup
/// failure into an actionable message. The snapshot-reading paths are exercised by the
/// Docker-backed integration tests.
/// </summary>
public class AspireDiagnosticsTests
{
    [Test]
    public async Task Classify_ExitCode137_IsOutOfMemory()
        => await Assert.That(Classify("Exited", 137, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.OutOfMemory);

    [Test]
    public async Task Classify_TerminalWithNonZeroExit_IsNonZeroExit()
        => await Assert.That(Classify("FailedToStart", 1, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.NonZeroExit);

    [Test]
    public async Task Classify_TerminalWithNoExitCode_IsCrashedNoCode()
        => await Assert.That(Classify("FailedToStart", null, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.CrashedNoCode);

    [Test]
    public async Task Classify_RunningButUnhealthy_IsHealthCheckFailing()
        => await Assert.That(Classify("Running", null, runningButUnhealthy: true, logLines: null))
            .IsEqualTo(ResourceFailureClass.HealthCheckFailing);

    [Test]
    public async Task Classify_Starting_IsNeverStarted()
        => await Assert.That(Classify("Starting", null, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.NeverStarted);

    [Test]
    public async Task Classify_Waiting_IsNeverStarted()
        => await Assert.That(Classify("Waiting", null, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.NeverStarted);

    [Test]
    public async Task Classify_RuntimeUnhealthyState_IsContainerRuntimeDown()
        => await Assert.That(Classify("RuntimeUnhealthy", null, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.ContainerRuntimeDown);

    [Test]
    public async Task Classify_DockerDaemonLogSignature_IsContainerRuntimeDown()
    {
        string[] logs = ["Cannot connect to the Docker daemon at unix:///var/run/docker.sock"];

        // Log signature wins even when the snapshot state is unknown.
        await Assert.That(Classify("unknown", null, runningButUnhealthy: false, logs))
            .IsEqualTo(ResourceFailureClass.ContainerRuntimeDown);
    }

    [Test]
    public async Task Classify_PortInUseLogSignature_IsPortInUse()
    {
        string[] logs = ["Error: bind: address already in use"];

        await Assert.That(Classify("FailedToStart", 1, runningButUnhealthy: false, logs))
            .IsEqualTo(ResourceFailureClass.PortInUse);
    }

    [Test]
    public async Task ScanLogSignatures_ImagePull_IsImagePullFailure()
        => await Assert.That(ScanLogSignatures(["Error response from daemon: pull access denied for foo/bar"]))
            .IsEqualTo(ResourceFailureClass.ImagePullFailure);

    [Test]
    public async Task ScanLogSignatures_NoSignature_IsNull()
        => await Assert.That(ScanLogSignatures(["starting up", "listening on port 8080"]))
            .IsNull();

    [Test]
    public async Task HintFor_KnownClass_IsActionable()
        => await Assert.That(HintFor(ResourceFailureClass.OutOfMemory)).Contains("OOM");

    [Test]
    public async Task HintFor_Unknown_IsNull()
        => await Assert.That(HintFor(ResourceFailureClass.Unknown)).IsNull();

    [Test]
    public async Task DescribeState_OutOfMemory_MentionsOom()
    {
        var diagnosis = new ResourceDiagnosis("cache", "Exited", 137, ResourceFailureClass.OutOfMemory, null, null);

        await Assert.That(DescribeState(diagnosis)).Contains("OOM / SIGKILL");
    }

    [Test]
    public async Task DescribeState_HealthCheckFailing_IncludesDetail()
    {
        var diagnosis = new ResourceDiagnosis(
            "api", "Running", null, ResourceFailureClass.HealthCheckFailing,
            Detail: "ready Unhealthy: connection refused", Hint: null);

        var description = DescribeState(diagnosis);

        await Assert.That(description).Contains("Running but not Healthy");
        await Assert.That(description).Contains("connection refused");
    }

    [Test]
    public async Task DescribeState_NeverStartedWithDependency_NamesDependency()
    {
        var diagnosis = new ResourceDiagnosis(
            "api", "Waiting", null, ResourceFailureClass.NeverStarted,
            Detail: "'migrations' (Exited, exit code 1)", Hint: null);

        await Assert.That(DescribeState(diagnosis)).Contains("waiting on 'migrations'");
    }

    [Test]
    public async Task FormatTimeline_NoEntries_IsEmpty()
        => await Assert.That(FormatTimeline([])).IsEmpty();

    [Test]
    public async Task FormatTimeline_SingleEntry_StillRenders()
    {
        StateTransition[] single = [new(TimeSpan.Zero, "api", "(none)", "FailedToStart", null)];

        await Assert.That(FormatTimeline(single)).Contains("[api] (none) -> FailedToStart");
    }

    [Test]
    public async Task FormatTimeline_MultipleEntries_RendersTransitions()
    {
        StateTransition[] transitions =
        [
            new(TimeSpan.Zero, "api", "(none)", "Starting", null),
            new(TimeSpan.FromSeconds(1.2), "api", "Starting", "Running", null),
            new(TimeSpan.FromSeconds(3.4), "api", "Running", "FailedToStart", null),
        ];

        var timeline = FormatTimeline(transitions);

        await Assert.That(timeline).Contains("[api] Starting -> Running");
        await Assert.That(timeline).Contains("FailedToStart");
    }
}
