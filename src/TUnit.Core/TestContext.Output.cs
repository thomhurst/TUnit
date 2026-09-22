using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal record TimingEntry(string StepName, DateTimeOffset Start, DateTimeOffset End)
{
    public TimeSpan Duration => End - Start;
}

/// <summary>
/// Test output capture and artifact management
/// Implements <see cref="ITestOutput"/> interface
/// </summary>
public partial class TestContext
{
    // Internal backing fields and properties.
    // Engine writes are sequential per-test (lifecycle-ordered).
    // User-facing writes via the obsolete ITestOutput.RecordTiming API may be concurrent,
    // so all access through the obsolete bridge takes _timingsLock.
    // Both lists are created on first write (most tests record neither) and double as their own
    // lock objects, so a passing test pays for no list or lock allocations here.
    private List<TimingEntry>? _timings;
    // Artifacts use a lock because AttachArtifact is user-facing and can be called
    // from parallel Task.WhenAll branches within a single test.
    private List<Artifact>? _artifacts;

    internal IReadOnlyList<TimingEntry> Timings =>
        (IReadOnlyList<TimingEntry>?)Volatile.Read(ref _timings) ?? Array.Empty<TimingEntry>();

    internal void ClearTimings()
    {
        if (Volatile.Read(ref _timings) is { } timings)
        {
            lock (timings) timings.Clear();
        }
    }

    internal IReadOnlyList<Artifact> Artifacts
    {
        get
        {
            if (Volatile.Read(ref _artifacts) is not { } artifacts)
            {
                return Array.Empty<Artifact>();
            }

            lock (artifacts)
            {
                return artifacts.Count == 0 ? Array.Empty<Artifact>() : [.. artifacts];
            }
        }
    }

    private List<Artifact> GetOrCreateArtifacts() => LazyInitializer.EnsureInitialized(ref _artifacts)!;

    // Explicit interface implementations for ITestOutput
    TextWriter ITestOutput.StandardOutput => OutputWriter;
    TextWriter ITestOutput.ErrorOutput => ErrorOutputWriter;
    IReadOnlyCollection<Artifact> ITestOutput.Artifacts => Artifacts;

#pragma warning disable CS0618 // Obsolete Timing API — bridge to internal TimingEntry storage
    IReadOnlyCollection<Timing> ITestOutput.Timings
    {
        get
        {
            if (Volatile.Read(ref _timings) is not { } timings)
            {
                return Array.Empty<Timing>();
            }

            lock (timings)
            {
                return timings.ConvertAll(t => new Timing(t.StepName, t.Start, t.End));
            }
        }
    }

    void ITestOutput.RecordTiming(Timing timing)
    {
        var timings = LazyInitializer.EnsureInitialized(ref _timings)!;
        lock (timings) timings.Add(new TimingEntry(timing.StepName, timing.Start, timing.End));
    }
#pragma warning restore CS0618

    void ITestOutput.AttachArtifact(Artifact artifact)
    {
        var artifacts = GetOrCreateArtifacts();
        lock (artifacts) artifacts.Add(artifact);
    }

    void ITestOutput.AttachArtifact(string filePath, string? displayName, string? description)
    {
        var fileInfo = new FileInfo(filePath);
        var artifact = new Artifact
        {
            File = fileInfo,
            DisplayName = displayName ?? fileInfo.Name,
            Description = description
        };
        var artifacts = GetOrCreateArtifacts();
        lock (artifacts) artifacts.Add(artifact);
    }

    string ITestOutput.GetStandardOutput() => GetOutput();
    string ITestOutput.GetErrorOutput() => GetOutputError();

    void ITestOutput.WriteLine(string message)
    {
        OutputWriter.WriteLine(message);
    }

    void ITestOutput.WriteError(string message)
    {
        ErrorOutputWriter.WriteLine(message);
    }

    /// <summary>
    /// Gets the combined build-time and execution-time standard output.
    /// </summary>
    public override string GetStandardOutput()
    {
        return GetOutput();
    }

    /// <summary>
    /// Gets the combined build-time and execution-time error output.
    /// </summary>
    public override string GetErrorOutput()
    {
        return GetOutputError();
    }

    internal string GetOutput() => CombineOutputs(_buildTimeOutput, base.GetStandardOutput());

    internal string GetOutputError() => CombineOutputs(_buildTimeErrorOutput, base.GetErrorOutput());

    internal override bool HasCapturedOutput =>
        base.HasCapturedOutput
        || !string.IsNullOrEmpty(_buildTimeOutput)
        || !string.IsNullOrEmpty(_buildTimeErrorOutput);

    private static string CombineOutputs(string? buildTimeOutput, string runtimeOutput)
    {
        if (string.IsNullOrEmpty(buildTimeOutput))
        {
            return runtimeOutput;
        }

        if (string.IsNullOrEmpty(runtimeOutput))
        {
            return buildTimeOutput!;
        }

        var vsb = new ValueStringBuilder(stackalloc char[256]);
        vsb.Append(buildTimeOutput);
        vsb.Append(Environment.NewLine);
        vsb.Append(runtimeOutput);
        return vsb.ToString();
    }

}
