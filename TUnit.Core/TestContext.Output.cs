using System.Collections.Concurrent;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test output capture and artifact management
/// Implements <see cref="ITestOutput"/> interface
/// </summary>
public partial class TestContext
{
    // Internal backing fields and properties
    internal ConcurrentBag<Timing> Timings { get; } = [];
    private readonly ConcurrentBag<Artifact> _artifactsBag = new();

    internal IReadOnlyList<Artifact> Artifacts => _artifactsBag.ToList();

    // Explicit interface implementations for ITestOutput
    TextWriter ITestOutput.StandardOutput => OutputWriter;
    TextWriter ITestOutput.ErrorOutput => ErrorOutputWriter;
    IReadOnlyCollection<Timing> ITestOutput.Timings => Timings;
    IReadOnlyCollection<Artifact> ITestOutput.Artifacts => Artifacts;

    void ITestOutput.RecordTiming(Timing timing)
    {
        Timings.Add(timing);
    }

    void ITestOutput.AttachArtifact(Artifact artifact)
    {
        _artifactsBag.Add(artifact);
    }

    void ITestOutput.AttachArtifact(string filePath, string? displayName, string? description)
    {
        var fileInfo = new FileInfo(filePath);
        _artifactsBag.Add(new Artifact
        {
            File = fileInfo,
            DisplayName = displayName ?? fileInfo.Name,
            Description = description
        });
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
