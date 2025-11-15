using System.Collections.Concurrent;
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
        var buildOutput = _buildTimeOutput ?? string.Empty;
        var baseOutput = base.GetStandardOutput();  // Get output from base class (Context)
        var writerOutput = _outputWriter?.ToString() ?? string.Empty;

        // Combine all three sources: build-time, base class output, and writer output
        var parts = new[] { buildOutput, baseOutput, writerOutput }
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(Environment.NewLine, parts);
    }

    internal string GetOutputError()
    {
        var buildErrorOutput = _buildTimeErrorOutput ?? string.Empty;
        var baseErrorOutput = base.GetErrorOutput();  // Get error output from base class (Context)
        var writerErrorOutput = _errorWriter?.ToString() ?? string.Empty;

        // Combine all three sources: build-time error, base class error output, and writer error output
        var parts = new[] { buildErrorOutput, baseErrorOutput, writerErrorOutput }
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(Environment.NewLine, parts);
    }
}
