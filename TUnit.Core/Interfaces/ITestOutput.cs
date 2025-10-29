namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test output capture and artifact management.
/// Accessed via <see cref="TestContext.Output"/>.
/// </summary>
public interface ITestOutput
{
    /// <summary>
    /// Gets the text writer for standard output.
    /// Use this for writing test progress, debugging information, or general output.
    /// Thread-safe for concurrent writes.
    /// </summary>
    TextWriter StandardOutput { get; }

    /// <summary>
    /// Gets the text writer for error output.
    /// Use this for writing error messages, warnings, or diagnostic information.
    /// Thread-safe for concurrent writes.
    /// </summary>
    TextWriter ErrorOutput { get; }

    /// <summary>
    /// Gets the collection of timing measurements recorded during test execution.
    /// Useful for performance profiling and identifying bottlenecks.
    /// </summary>
    IReadOnlyCollection<Timing> Timings { get; }

    /// <summary>
    /// Gets the collection of artifacts (files, screenshots, logs) attached to this test.
    /// Artifacts are preserved after test execution for review and debugging.
    /// </summary>
    IReadOnlyCollection<Artifact> Artifacts { get; }

    /// <summary>
    /// Records a timing measurement for a specific operation or phase.
    /// Thread-safe for concurrent calls.
    /// </summary>
    /// <param name="timing">The timing information to record</param>
    void RecordTiming(Timing timing);

    /// <summary>
    /// Attaches an artifact (file, screenshot, log, etc.) to this test.
    /// Artifacts are preserved after test execution.
    /// Thread-safe for concurrent calls.
    /// </summary>
    /// <param name="artifact">The artifact to attach</param>
    void AttachArtifact(Artifact artifact);

    /// <summary>
    /// Gets all standard output written during test execution as a single string.
    /// </summary>
    /// <returns>The accumulated standard output</returns>
    string GetStandardOutput();

    /// <summary>
    /// Gets all error output written during test execution as a single string.
    /// </summary>
    /// <returns>The accumulated error output</returns>
    string GetErrorOutput();
}
