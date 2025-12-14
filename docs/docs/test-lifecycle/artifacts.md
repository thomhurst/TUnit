# Test Artifacts

Test artifacts are files (screenshots, logs, videos, JSON dumps, etc.) that you can attach to your tests. They are invaluable for debugging test failures, especially in integration tests and end-to-end tests.

TUnit supports attaching artifacts at two levels:
- **Test-level artifacts**: Attached to individual tests
- **Session-level artifacts**: Attached to the entire test session

## Test-Level Artifacts

Attach files to individual tests using `TestContext.Current.Output.AttachArtifact()`.

### Basic Usage

```csharp
[Test]
public async Task MyIntegrationTest()
{
    // Perform your test logic
    var result = await PerformOperation();
    
    // Attach an artifact to this specific test
    TestContext.Current!.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo("path/to/logfile.log"),
        DisplayName = "Application Logs",
        Description = "Logs captured during test execution"
    });
    
    await Assert.That(result).IsEqualTo(expected);
}
```

### Attaching Screenshots on Failure

A common pattern is to capture a screenshot when a test fails:

```csharp
public class MyTests
{
    [After(HookType.Test)]
    public async Task TakeScreenshotOnFailure()
    {
        var testContext = TestContext.Current;
        
        if (testContext?.Result?.State == TestState.Failed)
        {
            // Capture screenshot
            var screenshotPath = await CaptureScreenshot();
            
            testContext.Output.AttachArtifact(new Artifact
            {
                File = new FileInfo(screenshotPath),
                DisplayName = "Failure Screenshot",
                Description = $"Screenshot captured when test '{testContext.TestDetails.TestName}' failed"
            });
        }
    }
    
    private async Task<string> CaptureScreenshot()
    {
        // Your screenshot capture logic
        var path = $"screenshots/test-{Guid.NewGuid()}.png";
        // ... capture screenshot to path ...
        return path;
    }
}
```

### Attaching Multiple Artifacts

You can attach multiple artifacts to a single test:

```csharp
[Test]
public async Task ComplexIntegrationTest()
{
    // Test logic that generates multiple outputs
    var httpLog = await ExecuteHttpRequests();
    var dbLog = await QueryDatabase();
    var traceLog = await CollectTraces();
    
    // Attach all artifacts
    TestContext.Current!.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(httpLog),
        DisplayName = "HTTP Requests",
        Description = "All HTTP requests and responses"
    });
    
    TestContext.Current.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(dbLog),
        DisplayName = "Database Queries",
        Description = "All database queries executed"
    });
    
    TestContext.Current.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(traceLog),
        DisplayName = "Trace Logs",
        Description = "Application trace logs"
    });
}
```

## Session-Level Artifacts

Attach files to the entire test session using `TestSessionContext.Current.AddArtifact()`. This is useful for artifacts that span multiple tests or provide context for the entire test run.

### Basic Usage

```csharp
[Before(HookType.TestSession)]
public static void SetupTestSession()
{
    // Start capturing session-wide logs
    var sessionLogPath = "test-session-log.txt";
    StartLogging(sessionLogPath);
    
    // This artifact is available to the entire test session
    TestSessionContext.Current!.AddArtifact(new Artifact
    {
        File = new FileInfo(sessionLogPath),
        DisplayName = "Test Session Log",
        Description = "Log file for the entire test session"
    });
}
```

### Configuration Files

Attach configuration files to document the test environment:

```csharp
[Before(HookType.TestSession)]
public static void DocumentTestEnvironment()
{
    // Attach environment configuration
    TestSessionContext.Current!.AddArtifact(new Artifact
    {
        File = new FileInfo("appsettings.test.json"),
        DisplayName = "Test Configuration",
        Description = "Application configuration used for this test run"
    });
    
    // Attach environment info
    var envInfo = CollectEnvironmentInfo();
    File.WriteAllText("environment-info.json", envInfo);
    
    TestSessionContext.Current.AddArtifact(new Artifact
    {
        File = new FileInfo("environment-info.json"),
        DisplayName = "Environment Information",
        Description = "System and runtime environment details"
    });
}
```

### Performance Reports

Generate and attach performance reports for the entire test session:

```csharp
[After(HookType.TestSession)]
public static void GeneratePerformanceReport()
{
    // Generate performance report after all tests complete
    var reportPath = "performance-report.html";
    GenerateReport(reportPath);
    
    TestSessionContext.Current!.AddArtifact(new Artifact
    {
        File = new FileInfo(reportPath),
        DisplayName = "Performance Report",
        Description = "Performance metrics for all tests in this session"
    });
}
```

## Artifact Class

The `Artifact` class has the following properties:

```csharp
public class Artifact
{
    public required FileInfo File { get; init; }          // The file to attach
    public required string DisplayName { get; init; }     // Human-readable name
    public string? Description { get; init; }             // Optional description
}
```

- **File**: A `FileInfo` object pointing to the file. The file must exist at the time of attachment.
- **DisplayName**: A short, descriptive name for the artifact (e.g., "Screenshot", "Logs", "Configuration").
- **Description**: An optional longer description providing more context about the artifact.

## Best Practices

### 1. Clean Up Artifacts

Consider cleaning up temporary artifact files after test execution to avoid accumulating files:

```csharp
[After(HookType.TestSession)]
public static void CleanupArtifacts()
{
    var artifactDir = "test-artifacts";
    if (Directory.Exists(artifactDir))
    {
        Directory.Delete(artifactDir, recursive: true);
    }
}
```

### 2. Organize Artifacts by Test

Create a unique directory for each test's artifacts:

```csharp
[Before(HookType.Test)]
public void SetupTestArtifactDirectory()
{
    var testName = TestContext.Current!.TestDetails.TestName;
    var sanitizedName = string.Concat(testName.Split(Path.GetInvalidFileNameChars()));
    var artifactDir = Path.Combine("test-artifacts", sanitizedName);
    Directory.CreateDirectory(artifactDir);
    
    TestContext.Current.StateBag["ArtifactDir"] = artifactDir;
}

[Test]
public void MyTest()
{
    var artifactDir = (string)TestContext.Current!.StateBag["ArtifactDir"];
    var logPath = Path.Combine(artifactDir, "test.log");
    
    // ... test logic ...
    
    TestContext.Current.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(logPath),
        DisplayName = "Test Log"
    });
}
```

### 3. Only Attach on Failure

For large artifacts (videos, extensive logs), consider only attaching them when tests fail:

```csharp
[After(HookType.Test)]
public async Task ConditionalArtifactAttachment()
{
    var testContext = TestContext.Current;
    
    if (testContext?.Result?.State is TestState.Failed or TestState.TimedOut)
    {
        // Only attach expensive artifacts on failure
        var videoPath = await StopRecording();
        
        testContext.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo(videoPath),
            DisplayName = "Test Recording",
            Description = "Video recording of the failed test"
        });
    }
}
```

### 4. Use Descriptive Names

Provide clear, descriptive names and descriptions for your artifacts:

```csharp
// ❌ Not descriptive
TestContext.Current!.Output.AttachArtifact(new Artifact
{
    File = new FileInfo("log.txt"),
    DisplayName = "Log"
});

// ✅ Descriptive and helpful
TestContext.Current!.Output.AttachArtifact(new Artifact
{
    File = new FileInfo("http-trace.log"),
    DisplayName = "HTTP Request Trace",
    Description = "Complete trace of all HTTP requests including headers and response bodies"
});
```

### 5. Verify Files Exist

Always ensure the file exists before attaching:

```csharp
var logPath = "path/to/logfile.log";

if (File.Exists(logPath))
{
    TestContext.Current!.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(logPath),
        DisplayName = "Application Log"
    });
}
else
{
    TestContext.Current!.Output.WriteLine($"Warning: Log file not found at {logPath}");
}
```

## Common Use Cases

### Browser Testing with Playwright

```csharp
[After(HookType.Test)]
public async Task CapturePlaywrightArtifacts()
{
    var testContext = TestContext.Current;
    
    if (testContext?.Result?.State != TestState.Passed)
    {
        // Capture screenshot
        var screenshotPath = $"artifacts/screenshot-{testContext.Id}.png";
        await _page.ScreenshotAsync(new() { Path = screenshotPath });
        
        testContext.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo(screenshotPath),
            DisplayName = "Browser Screenshot"
        });
        
        // Capture video if enabled
        if (_browserContext.Options?.RecordVideo != null)
        {
            await _page.CloseAsync();
            var videoPath = await _page.Video!.PathAsync();
            
            testContext.Output.AttachArtifact(new Artifact
            {
                File = new FileInfo(videoPath),
                DisplayName = "Browser Recording"
            });
        }
    }
}
```

### API Testing

```csharp
[Test]
public async Task ApiIntegrationTest()
{
    var requestLog = new StringBuilder();
    var responseLog = new StringBuilder();
    
    // Make API calls while logging
    var response = await _httpClient.GetAsync("/api/endpoint");
    requestLog.AppendLine($"GET /api/endpoint");
    responseLog.AppendLine(await response.Content.ReadAsStringAsync());
    
    // Save and attach logs
    var requestPath = "api-request.txt";
    var responsePath = "api-response.txt";
    
    await File.WriteAllTextAsync(requestPath, requestLog.ToString());
    await File.WriteAllTextAsync(responsePath, responseLog.ToString());
    
    TestContext.Current!.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(requestPath),
        DisplayName = "API Request"
    });
    
    TestContext.Current.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(responsePath),
        DisplayName = "API Response"
    });
}
```

### Database Testing

```csharp
[Test]
public async Task DatabaseIntegrationTest()
{
    var queryLog = new List<string>();
    
    // Execute queries while logging
    foreach (var query in _queries)
    {
        await _connection.ExecuteAsync(query);
        queryLog.Add(query);
    }
    
    // Save query log
    var logPath = "database-queries.sql";
    await File.WriteAllLinesAsync(logPath, queryLog);
    
    TestContext.Current!.Output.AttachArtifact(new Artifact
    {
        File = new FileInfo(logPath),
        DisplayName = "Database Queries",
        Description = "All SQL queries executed during test"
    });
}
```

## Integration with Test Runners

Artifacts attached using TUnit are automatically forwarded to the underlying Microsoft.Testing.Platform infrastructure, which makes them available to:

- Test result files (TRX, etc.)
- CI/CD systems (GitHub Actions, Azure DevOps, etc.)
- Test explorers in IDEs (Visual Studio, Rider, VS Code)

The exact behavior depends on your test runner configuration and CI/CD platform.

## See Also

- [Test Context](./test-context.md) - Overview of TestContext
- [Test Lifecycle Hooks](./setup.md) - Using Before/After hooks
- [CI/CD Reporting](../execution/ci-cd-reporting.md) - Integrating with CI systems
