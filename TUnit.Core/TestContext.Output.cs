using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test output capture and artifact management
/// Implements <see cref="ITestOutput"/> interface
/// </summary>
public partial class TestContext
{
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
    string ITestOutput.GetErrorOutput() => GetErrorOutput();
}
