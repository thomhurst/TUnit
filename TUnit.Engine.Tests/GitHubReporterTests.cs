using Microsoft.Testing.Platform.Extensions;
using Shouldly;
using TUnit.Engine.Reporters;
using System.Reflection;
using TUnit.Engine.Helpers;

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
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
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
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
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
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
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
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
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

    [Test]
    public async Task WriteFile_Should_Retry_On_IOException()
    {
        // Arrange
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        var tempFile = CreateTempFile();
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", tempFile);
        
        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);
        await reporter.IsEnabledAsync();

        // Open the file exclusively to simulate file locking
        using var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Write, FileShare.None);
        
        try
        {
            // Act - Use reflection to access the private WriteFile method
            var writeFileMethod = typeof(GitHubReporter).GetMethod("WriteFile", BindingFlags.NonPublic | BindingFlags.Instance);
            writeFileMethod.ShouldNotBeNull();

            // Start the write operation asynchronously
            var writeTask = (Task?)writeFileMethod.Invoke(reporter, ["test content"]);
            writeTask.ShouldNotBeNull();
            
            // Release the file lock after a short delay to allow one retry to succeed
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                fileStream.Close();
            });

            // Should not throw and should complete successfully after retry
            await writeTask;
            
            // Verify content was written after the lock was released
            var content = await File.ReadAllTextAsync(tempFile);
            content.ShouldContain("test content");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Test] 
    public async Task WriteFile_Should_Handle_Permanent_File_Lock_Gracefully()
    {
        // Arrange
        EnvironmentVariableCache.ClearCache(); // Clear cache to pick up new environment variables
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        var tempFile = CreateTempFile();
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", tempFile);
        
        var extension = new MockExtension();
        var reporter = new GitHubReporter(extension);
        await reporter.IsEnabledAsync();

        // Open the file exclusively and keep it locked
        using var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Write, FileShare.None);
        
        try
        {
            // Act - Use reflection to access the private WriteFile method
            var writeFileMethod = typeof(GitHubReporter).GetMethod("WriteFile", BindingFlags.NonPublic | BindingFlags.Instance);
            writeFileMethod.ShouldNotBeNull();

            var writeTask = (Task?)writeFileMethod.Invoke(reporter, ["test content"]);
            writeTask.ShouldNotBeNull();
            
            // Should not throw even when all retries are exhausted
            await writeTask;
            
            // Should complete without throwing exceptions
            writeTask.IsCompletedSuccessfully.ShouldBeTrue();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", null);
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", null);
            try { File.Delete(tempFile); } catch { }
        }
    }
}