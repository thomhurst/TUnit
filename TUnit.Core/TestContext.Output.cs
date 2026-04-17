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
    internal List<TimingEntry> Timings { get; } = [];
    private readonly Lock _timingsLock = new();
    // Artifacts use a lock because AttachArtifact is user-facing and can be called
    // from parallel Task.WhenAll branches within a single test.
    private readonly Lock _artifactsLock = new();
    private readonly List<Artifact> _artifacts = [];

    internal IReadOnlyList<Artifact> Artifacts { get { lock (_artifactsLock) return [.. _artifacts]; } }

    // Explicit interface implementations for ITestOutput
    TextWriter ITestOutput.StandardOutput => OutputWriter;
    TextWriter ITestOutput.ErrorOutput => ErrorOutputWriter;
    IReadOnlyCollection<Artifact> ITestOutput.Artifacts => Artifacts;

#pragma warning disable CS0618 // Obsolete Timing API — bridge to internal TimingEntry storage
    IReadOnlyCollection<Timing> ITestOutput.Timings
    {
        get
        {
            lock (_timingsLock)
            {
                return Timings.ConvertAll(t => new Timing(t.StepName, t.Start, t.End));
            }
        }
    }

    void ITestOutput.RecordTiming(Timing timing)
    {
        lock (_timingsLock) Timings.Add(new TimingEntry(timing.StepName, timing.Start, timing.End));
    }
#pragma warning restore CS0618

    void ITestOutput.AttachArtifact(Artifact artifact)
    {
        lock (_artifactsLock) _artifacts.Add(artifact);
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
        lock (_artifactsLock) _artifacts.Add(artifact);
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
        base.HasCapturedOutput || _buildTimeOutput != null || _buildTimeErrorOutput != null;

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
