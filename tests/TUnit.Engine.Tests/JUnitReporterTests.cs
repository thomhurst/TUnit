using System.Xml.Linq;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Core;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.Tests;

[NotInParallel]
public class JUnitReporterTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AfterRunAsync_Should_Preserve_Results_When_Run_Is_Cancelled(bool cancelRun)
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), "TUnit-JUnit-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "results.xml");
        Environment.SetEnvironmentVariable("TUNIT_ENABLE_JUNIT_REPORTER", "true");
        Environment.SetEnvironmentVariable("JUNIT_XML_OUTPUT_PATH", path);
        var reporter = new JUnitReporter(new MockExtension());
        using var cancellation = new CancellationTokenSource();

        try
        {
            (await reporter.IsEnabledAsync()).ShouldBeTrue();
            await AddResult("Passed", PassedTestNodeStateProperty.CachedInstance);
            await AddResult("Failed", new FailedTestNodeStateProperty(new InvalidOperationException("Test body failure")));
            await AddResult("Unfinished", new InProgressTestNodeStateProperty());
            if (cancelRun)
            {
                cancellation.Cancel();
            }

            // Act
            await reporter.AfterRunAsync(exitCode: 3, cancellation.Token);

            // Assert
            var cases = XDocument.Load(path).Descendants("testcase").ToArray();
            cases.Length.ShouldBe(3);
            cases.Single(x => (string?)x.Attribute("name") == "Passed").HasElements.ShouldBeFalse();
            cases.Single(x => (string?)x.Attribute("name") == "Failed").Element("failure")!
                .Attribute("message")!.Value.ShouldContain("Test body failure");
            cases.Single(x => (string?)x.Attribute("name") == "Unfinished").Element("error")!
                .Attribute("message")!.Value.ShouldContain("Test never finished");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        Task AddResult(string name, TestNodeStateProperty state) => reporter.ConsumeAsync(
            null!,
            new TestNodeUpdateMessage(new SessionUid("cancelled-session"), new TestNode
            {
                Uid = new TestNodeUid(name),
                DisplayName = name,
                Properties = new PropertyBag(state)
            }),
            CancellationToken.None);
    }

    [After(Test)]
    public void Cleanup()
    {
        // Clean up environment variables after each test
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JUNIT_REPORTER", null);
        Environment.SetEnvironmentVariable("TUNIT_ENABLE_JUNIT_REPORTER", null);
        Environment.SetEnvironmentVariable("GITLAB_CI", null);
        Environment.SetEnvironmentVariable("CI_SERVER", null);
        Environment.SetEnvironmentVariable("JUNIT_XML_OUTPUT_PATH", null);
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_TUNIT_DISABLE_JUNIT_REPORTER_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JUNIT_REPORTER", "true");
        Environment.SetEnvironmentVariable("GITLAB_CI", "true"); // Even with GitLab CI, should be disabled
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_True_When_GITLAB_CI_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITLAB_CI", "true");
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_True_When_CI_SERVER_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CI_SERVER", "yes");
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_True_When_TUNIT_ENABLE_JUNIT_REPORTER_Is_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_ENABLE_JUNIT_REPORTER", "true");
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Return_False_When_No_Environment_Variables_Are_Set()
    {
        // Arrange
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_Prefer_Disable_Over_Enable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JUNIT_REPORTER", "true");
        Environment.SetEnvironmentVariable("TUNIT_ENABLE_JUNIT_REPORTER", "true");
        var extension = new MockExtension();
        var reporter = new JUnitReporter(extension);

        // Act
        var isEnabled = await reporter.IsEnabledAsync();

        // Assert
        await Assert.That(isEnabled).IsFalse();
    }
}
