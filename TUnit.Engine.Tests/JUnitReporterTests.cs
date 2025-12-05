using Microsoft.Testing.Platform.Extensions;
using TUnit.Core;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.Tests;

[NotInParallel]
public class JUnitReporterTests
{
    private sealed class MockExtension : IExtension
    {
        public string Uid => "MockExtension";
        public string DisplayName => "Mock";
        public string Version => "1.0.0";
        public string Description => "Mock Extension";
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
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
