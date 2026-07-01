using Aspire.Hosting.ApplicationModel;
using TUnit.Aspire.Tests.Helpers;
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

    // A crashed .NET project reaches Finished (not Exited) with a non-zero exit code — #6342.
    [Test]
    public async Task Classify_FinishedWithNonZeroExit_IsNonZeroExit()
        => await Assert.That(Classify("Finished", -532462766, runningButUnhealthy: false, logLines: null))
            .IsEqualTo(ResourceFailureClass.NonZeroExit);

    // --- IsFailureState: the fail-fast trigger. #6342 = Finished + non-zero was previously missed. ---

    [Test]
    public async Task IsFailureState_FinishedWithNonZeroExit_IsFailure()
        => await Assert.That(IsFailureState("Finished", -532462766)).IsTrue();

    [Test]
    public async Task IsFailureState_ExitedWithNonZeroExit_IsFailure()
        => await Assert.That(IsFailureState("Exited", 1)).IsTrue();

    [Test]
    public async Task IsFailureState_FailedToStart_IsFailure()
        => await Assert.That(IsFailureState("FailedToStart", null)).IsTrue();

    // A one-shot resource (migration runner, seeder) that finishes cleanly must NOT be a failure.
    [Test]
    public async Task IsFailureState_FinishedWithZeroExit_IsNotFailure()
        => await Assert.That(IsFailureState("Finished", 0)).IsFalse();

    [Test]
    public async Task IsFailureState_ExitedWithZeroExit_IsNotFailure()
        => await Assert.That(IsFailureState("Exited", 0)).IsFalse();

    [Test]
    public async Task IsFailureState_Running_IsNotFailure()
        => await Assert.That(IsFailureState("Running", null)).IsFalse();

    [Test]
    public async Task IsFailureState_NullState_IsNotFailure()
        => await Assert.That(IsFailureState(null, null)).IsFalse();

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

    // --- Exit-code decoding (#6342): turn opaque numbers into human-readable causes. ---

    [Test]
    public async Task DecodeExitCode_DotNetUnhandledException_IsDescribed()
        => await Assert.That(DecodeExitCode(-532462766)).Contains(".NET unhandled exception");

    [Test]
    public async Task DecodeExitCode_AccessViolation_IsDescribed()
        => await Assert.That(DecodeExitCode(unchecked((int)0xC0000005))).Contains("access violation");

    [Test]
    public async Task DecodeExitCode_Sigkill_MentionsOom()
        => await Assert.That(DecodeExitCode(137)).Contains("SIGKILL");

    [Test]
    public async Task DecodeExitCode_UnknownCode_IsNull()
        => await Assert.That(DecodeExitCode(1)).IsNull();

    [Test]
    public async Task FormatExitCode_KnownCode_IncludesNumberAndMeaning()
    {
        var formatted = FormatExitCode(-532462766);

        await Assert.That(formatted).Contains("-532462766");
        await Assert.That(formatted).Contains(".NET unhandled exception");
    }

    [Test]
    public async Task FormatExitCode_UnknownCode_IsBareNumber()
        => await Assert.That(FormatExitCode(1)).IsEqualTo("1");

    [Test]
    public async Task FormatExitCode_Null_IsUnknown()
        => await Assert.That(FormatExitCode(null)).IsEqualTo("(unknown)");

    [Test]
    public async Task DescribeState_NonZeroExit_DecodesExitCode()
    {
        var diagnosis = new ResourceDiagnosis(
            "chat", "Finished", -532462766, ResourceFailureClass.NonZeroExit, null, null);

        var description = DescribeState(diagnosis);

        await Assert.That(description).Contains("Finished");
        await Assert.That(description).Contains(".NET unhandled exception");
    }

    // --- Reverse dependency graph (#6342): who was awaiting the failed resource. ---

    [Test]
    public async Task FindAwaiters_ReturnsResourcesThatWaitOnTarget()
    {
        var chat = new FakeComputeResource("chat");
        var media = new FakeComputeResource("media");
        var web = new FakeComputeResource("web");
        var unrelated = new FakeComputeResource("db");

        media.Annotations.Add(new WaitAnnotation(chat, WaitType.WaitUntilHealthy, 0));
        web.Annotations.Add(new WaitAnnotation(chat, WaitType.WaitUntilHealthy, 0));

        var awaiters = FindAwaiters([chat, media, web, unrelated], "chat");

        await Assert.That(awaiters).Contains("media");
        await Assert.That(awaiters).Contains("web");
        await Assert.That(awaiters).DoesNotContain("db");
        await Assert.That(awaiters).DoesNotContain("chat");
    }

    [Test]
    public async Task FindAwaiters_NoWaiters_IsEmpty()
    {
        var chat = new FakeComputeResource("chat");
        var db = new FakeComputeResource("db");

        await Assert.That(FindAwaiters([chat, db], "chat")).IsEmpty();
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
    public async Task TrimTimeline_KeepsHeadAndTail_DropsMiddle()
    {
        var list = new List<StateTransition>();
        for (var i = 0; i < 400; i++)
        {
            list.Add(new StateTransition(TimeSpan.FromSeconds(i), "r", i.ToString(), (i + 1).ToString(), null));
        }

        TrimTimeline(list, head: 80, tail: 120);

        await Assert.That(list.Count).IsEqualTo(200);
        await Assert.That(list[0].From).IsEqualTo("0");      // earliest startup transition preserved
        await Assert.That(list[^1].To).IsEqualTo("400");     // most recent transition preserved
    }

    [Test]
    public async Task TrimTimeline_UnderCap_LeavesUnchanged()
    {
        var list = new List<StateTransition>
        {
            new(TimeSpan.Zero, "r", "(none)", "Starting", null),
            new(TimeSpan.FromSeconds(1), "r", "Starting", "Running", null),
        };

        TrimTimeline(list, head: 80, tail: 120);

        await Assert.That(list.Count).IsEqualTo(2);
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
