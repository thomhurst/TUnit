#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.Tests;

[NotInParallel]
public class GitHubReporterTests
{
    private readonly List<string> _tempFiles = [];

    [After(Test)]
    public void CleanupAfterTest()
    {
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", null);
        Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", null);
        Environment.SetEnvironmentVariable("TUNIT_GITHUB_REPORTER_STYLE", null);
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
        Environment.SetEnvironmentVariable("GITHUB_SHA", null);

        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { /* best-effort cleanup */ }
        }
        _tempFiles.Clear();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_TUNIT_DISABLE_GITHUB_REPORTER_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", "true");
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", CreateTempFile());

        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);

        // Act
        var result = await reporter.IsEnabledAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_DISABLE_GITHUB_REPORTER_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", "true");
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", CreateTempFile());

        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);

        // Act
        var result = await reporter.IsEnabledAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_Both_Environment_Variables_Are_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", "true");
        Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", "true");
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", CreateTempFile());

        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);

        // Act
        var result = await reporter.IsEnabledAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_GITHUB_ACTIONS_Is_Not_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", null);
        Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", null);
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", CreateTempFile());

        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);

        // Act
        var result = await reporter.IsEnabledAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task AfterRunAsync_Groups_Failures_By_Exception_Type()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "TestA", "MyService", new NullReferenceException("obj was null")),
            CreateFailedTestMessage("2", "TestB", "MyService", new NullReferenceException("another null")),
            CreateFailedTestMessage("3", "TestC", "OtherService", new ArgumentException("bad arg"))
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("Failures by Cause");
        output.ShouldContain("NullReferenceException (2 tests)");
        output.ShouldContain("ArgumentException (1 test)");
        output.ShouldContain("`MyService.TestA`");
        output.ShouldContain("`MyService.TestB`");
        output.ShouldContain("`OtherService.TestC`");
    }

    [Test]
    public async Task AfterRunAsync_Orders_Groups_By_Count_Descending()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "T1", "Svc", new ArgumentException("a")),
            CreateFailedTestMessage("2", "T2", "Svc", new NullReferenceException("n1")),
            CreateFailedTestMessage("3", "T3", "Svc", new NullReferenceException("n2")),
            CreateFailedTestMessage("4", "T4", "Svc", new NullReferenceException("n3"))
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        var nreIndex = output.IndexOf("NullReferenceException (3 tests)", StringComparison.Ordinal);
        var argIndex = output.IndexOf("ArgumentException (1 test)", StringComparison.Ordinal);
        nreIndex.ShouldBeLessThan(argIndex, "NullReferenceException group (3) should appear before ArgumentException group (1)");
    }

    [Test]
    public async Task AfterRunAsync_Groups_Timeouts_As_Timeout()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateTimeoutTestMessage("1", "SlowTest1", "MyService"),
            CreateTimeoutTestMessage("2", "SlowTest2", "MyService")
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("Timeout (2 tests)");
        output.ShouldContain("`MyService.SlowTest1`");
        output.ShouldContain("`MyService.SlowTest2`");
    }

    [Test]
    public async Task AfterRunAsync_Collapsible_Style_Wraps_Groups_In_Details()
    {
        var (reporter, outputFile) = await SetupReporter(GitHubReporterStyle.Collapsible);

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "T1", "Svc", new InvalidOperationException("oops"))
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("<details>");
        output.ShouldContain("<summary>InvalidOperationException (1 test)</summary>");
        output.ShouldContain("</details>");
    }

    [Test]
    public async Task AfterRunAsync_Full_Style_Renders_Groups_Expanded()
    {
        var (reporter, outputFile) = await SetupReporter(GitHubReporterStyle.Full);

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "T1", "Svc", new InvalidOperationException("oops"))
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("**InvalidOperationException (1 test)**");
        // Full mode should not wrap failure groups in <details>
        // The output contains <details> for other sections, but the failure group itself should use **bold**
        output.ShouldContain("| `Svc.T1`");
    }

    [Test]
    public async Task AfterRunAsync_Shows_Common_Error_For_Each_Group()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "T1", "Svc", new NullReferenceException("Object reference not set")),
            CreateFailedTestMessage("2", "T2", "Svc", new NullReferenceException("Different message"))
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("**Common error:**");
        // The common error is from the first entry in the group (order not guaranteed),
        // so check that at least one of the messages appears
        (output.Contains("Object reference not set") || output.Contains("Different message"))
            .ShouldBeTrue("Common error should contain one of the exception messages");
    }

    [Test]
    public async Task AfterRunAsync_Quick_Diagnosis_Includes_Timeouts()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "T1", "Svc", new NullReferenceException("n")),
            CreateTimeoutTestMessage("2", "SlowTest", "Svc")
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        output.ShouldContain("Quick diagnosis:");
        output.ShouldContain("Timeout");
    }

    [Test]
    public async Task AfterRunAsync_Other_NonPassing_Tests_Remain_Separate()
    {
        var (reporter, outputFile) = await SetupReporter();

        await FeedTestMessages(reporter,
            CreateFailedTestMessage("1", "FailedTest", "Svc", new Exception("err")),
            CreatePassedTestMessage("2", "PassedTest", "Svc"),
            CreateCancelledTestMessage("3", "CancelledTest", "Svc")
        );

        await reporter.AfterRunAsync(1, CancellationToken.None);

        var output = await File.ReadAllTextAsync(outputFile);
        // Failures in grouped section
        output.ShouldContain("Failures by Cause");
        output.ShouldContain("`Svc.FailedTest`");
        // Cancelled test in the other table
        output.ShouldContain("Other non-passing tests");
        output.ShouldContain("CancelledTest");
    }

    private string CreateTempFile()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        return path;
    }

    private async Task<(GitHubReporter Reporter, string OutputFile)> SetupReporter(
        GitHubReporterStyle style = GitHubReporterStyle.Collapsible)
    {
        var outputFile = CreateTempFile();
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", outputFile);

        var reporter = new GitHubReporter(new MockExtension());
        await reporter.IsEnabledAsync();
        reporter.SetReporterStyle(style);
        await reporter.BeforeRunAsync(CancellationToken.None);

        return (reporter, outputFile);
    }

    private static async Task FeedTestMessages(GitHubReporter reporter, params TestNodeUpdateMessage[] messages)
    {
        foreach (var message in messages)
        {
            await reporter.ConsumeAsync(null!, message, CancellationToken.None);
        }
    }

    private static TestNodeUpdateMessage CreateTestMessage(
        string testId, string displayName, string typeName, IProperty stateProperty)
    {
        return new TestNodeUpdateMessage(
            sessionUid: new SessionUid("test-session"),
            testNode: new TestNode
            {
                Uid = new TestNodeUid(testId),
                DisplayName = displayName,
                Properties = new PropertyBag(
                    stateProperty,
                    new TestMethodIdentifierProperty(
                        @namespace: "TestNamespace",
                        assemblyFullName: "TestAssembly",
                        typeName: typeName,
                        methodName: displayName,
                        parameterTypeFullNames: [],
                        returnTypeFullName: "System.Void",
                        methodArity: 0))
            });
    }

    private static TestNodeUpdateMessage CreateFailedTestMessage(
        string testId, string displayName, string typeName, Exception exception) =>
        CreateTestMessage(testId, displayName, typeName, new FailedTestNodeStateProperty(exception, exception.Message));

    private static TestNodeUpdateMessage CreateTimeoutTestMessage(
        string testId, string displayName, string typeName) =>
        CreateTestMessage(testId, displayName, typeName, new TimeoutTestNodeStateProperty("Test timed out after 30s"));

    private static TestNodeUpdateMessage CreatePassedTestMessage(
        string testId, string displayName, string typeName) =>
        CreateTestMessage(testId, displayName, typeName, PassedTestNodeStateProperty.CachedInstance);

#pragma warning disable CS0618
    private static TestNodeUpdateMessage CreateCancelledTestMessage(
        string testId, string displayName, string typeName) =>
        CreateTestMessage(testId, displayName, typeName, new CancelledTestNodeStateProperty());
#pragma warning restore CS0618
}
