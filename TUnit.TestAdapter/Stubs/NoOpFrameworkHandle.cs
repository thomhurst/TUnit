using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TUnit.TestAdapter.Stubs;

public class NoOpFrameworkHandle : IFrameworkHandle
{
    public void SendMessage(TestMessageLevel testMessageLevel, string message)
    {
    }

    public void RecordResult(TestResult testResult)
    {
    }

    public void RecordStart(TestCase testCase)
    {
    }

    public void RecordEnd(TestCase testCase, TestOutcome outcome)
    {
    }

    public void RecordAttachments(IList<AttachmentSet> attachmentSets)
    {
    }

    public int LaunchProcessWithDebuggerAttached(string filePath, string? workingDirectory, string? arguments,
        IDictionary<string, string?>? environmentVariables)
    {
        return 0;
    }

    public bool EnableShutdownAfterTestRun { get; set; } = true;
}