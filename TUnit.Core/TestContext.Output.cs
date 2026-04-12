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
    // Internal backing fields and properties
    // Timings are written sequentially by the framework during test execution, never by user code.
    internal List<TimingEntry> Timings { get; } = [];
    // Artifacts use a lock because AttachArtifact is user-facing and can be called
    // from parallel Task.WhenAll branches within a single test.
    private readonly Lock _artifactsLock = new();
    private readonly List<Artifact> _artifacts = [];

    internal IReadOnlyList<Artifact> Artifacts { get { lock (_artifactsLock) return [.. _artifacts]; } }

    // Explicit interface implementations for ITestOutput
    TextWriter ITestOutput.StandardOutput => OutputWriter;
    TextWriter ITestOutput.ErrorOutput => ErrorOutputWriter;
    IReadOnlyCollection<Artifact> ITestOutput.Artifacts => Artifacts;

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
        _outputWriter ??= new StringWriter();
        _outputWriter.WriteLine(message);
    }

    void ITestOutput.WriteError(string message)
    {
        _errorWriter ??= new StringWriter();
        _errorWriter.WriteLine(message);
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

    internal string GetOutput()
    {
        var vsb = new ValueStringBuilder(stackalloc char[256]);

        var buildOutput = _buildTimeOutput ?? string.Empty;
        var baseOutput = base.GetStandardOutput();  // Get output from base class (Context)
        var writerOutput = _outputWriter?.ToString() ?? string.Empty;

        AppendIfNotNullOrEmpty(ref vsb, buildOutput);
        AppendIfNotNullOrEmpty(ref vsb, baseOutput);
        AppendIfNotNullOrEmpty(ref vsb, writerOutput);

        return vsb.ToString();
    }

    internal string GetOutputError()
    {
        var vsb = new ValueStringBuilder(stackalloc char[256]);

        var buildErrorOutput = _buildTimeErrorOutput ?? string.Empty;
        var baseErrorOutput = base.GetErrorOutput();  // Get error output from base class (Context)
        var writerErrorOutput = _errorWriter?.ToString() ?? string.Empty;

        AppendIfNotNullOrEmpty(ref vsb, buildErrorOutput);
        AppendIfNotNullOrEmpty(ref vsb, baseErrorOutput);
        AppendIfNotNullOrEmpty(ref vsb, writerErrorOutput);

        return vsb.ToString();
    }

    private static void AppendIfNotNullOrEmpty(ref ValueStringBuilder builder, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(Environment.NewLine);
        }
        builder.Append(value);
    }
}
