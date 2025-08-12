using Microsoft.Testing.Platform.Extensions;
using Shouldly;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.Tests;

public class GitHubReporterTests
{
    private sealed class MockExtension : IExtension
    {
        public string Uid => "MockExtension";
        public string DisplayName => "Mock";
        public string Version => "1.0.0";
        public string Description => "Mock Extension";
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
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
        
        try
        {
            // Act
            var result = await reporter.IsEnabledAsync();
            
            // Assert
            result.ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", null);
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        }
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
        
        try
        {
            // Act
            var result = await reporter.IsEnabledAsync();
            
            // Assert
            result.ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", null);
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        }
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
        
        try
        {
            // Act
            var result = await reporter.IsEnabledAsync();
            
            // Assert
            result.ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TUNIT_DISABLE_GITHUB_REPORTER", null);
            Environment.SetEnvironmentVariable("DISABLE_GITHUB_REPORTER", null);
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        }
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
        
        try
        {
            // Act
            var result = await reporter.IsEnabledAsync();
            
            // Assert
            result.ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
        }
    }
    
    private static string CreateTempFile()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");
        return tempFile;
    }
}