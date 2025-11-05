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
    private List<Artifact>? _cachedArtifactsList;
    private int _artifactsVersion = 0;

    internal IReadOnlyList<Artifact> Artifacts
    {
        get
        {
            // Simple version check - if bag hasn't changed, return cached list
            if (_cachedArtifactsList != null && _artifactsVersion == _artifactsBag.Count)
            {
                return _cachedArtifactsList;
            }

            _cachedArtifactsList = _artifactsBag.ToList();
            _artifactsVersion = _artifactsBag.Count;
            return _cachedArtifactsList;
        }
    }

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
        _artifactsVersion++; // Invalidate cache
    }

    string ITestOutput.GetStandardOutput() => GetOutput();
    string ITestOutput.GetErrorOutput() => GetErrorOutput();

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

    internal string GetOutput() => _outputWriter?.ToString() ?? string.Empty;

    internal new string GetErrorOutput() => _errorWriter?.ToString() ?? string.Empty;
}
