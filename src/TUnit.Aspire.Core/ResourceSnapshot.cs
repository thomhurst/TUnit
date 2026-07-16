namespace TUnit.Aspire;

/// <summary>
/// A point-in-time view of an Aspire resource's lifecycle state, returned by
/// <see cref="AspireFixture{TAppHost}.GetResourceSnapshot"/>. A lightweight, dependency-free
/// projection of Aspire's internal snapshot so tests can introspect a resource without taking a
/// direct dependency on <c>Aspire.Hosting</c> types.
/// </summary>
/// <param name="Name">The resource name.</param>
/// <param name="State">
/// The current state text (e.g. <c>Running</c>, <c>Finished</c>, <c>Exited</c>, <c>FailedToStart</c>),
/// or <c>null</c> when the resource has not yet reported a state.
/// </param>
/// <param name="ExitCode">The process exit code when the resource has terminated; otherwise <c>null</c>.</param>
/// <param name="StartedAt">When the resource started, or <c>null</c> if it has not started.</param>
/// <param name="StoppedAt">When the resource stopped, or <c>null</c> if it is still running.</param>
/// <param name="HealthStatus">
/// The aggregate health status text (e.g. <c>Healthy</c>, <c>Unhealthy</c>), or <c>null</c> when the
/// resource has no health checks or is not in a state that reports health.
/// </param>
public sealed record ResourceSnapshot(
    string Name,
    string? State,
    int? ExitCode,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    string? HealthStatus);
