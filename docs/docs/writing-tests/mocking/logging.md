---
sidebar_position: 7
---


# Logging

`TUnit.Mocks.Logging` provides `MockLogger` — a simple `ILogger` implementation that captures log entries for inspection and verification.

```bash
dotnet add package TUnit.Mocks.Logging
```

:::tip No Source Generation Needed
Unlike `TUnit.Mocks`, the logging helpers are plain classes — no source generation required. Create loggers with `Mock.Logger()` and pass them to your code.
:::

## Getting Started

```csharp
using TUnit.Mocks;
using Microsoft.Extensions.Logging;

[Test]
public async Task Service_Logs_On_Startup()
{
    // Arrange
    var logger = Mock.Logger();
    var service = new MyService(logger);

    // Act
    service.Start();

    // Assert
    logger.VerifyLog(Microsoft.Extensions.Logging.LogLevel.Information, "started", Times.Once);
}
```

## Creating a Logger

```csharp
// Untyped logger
var untypedLogger = Mock.Logger();
Microsoft.Extensions.Logging.ILogger untypedILogger = untypedLogger;

// With category name
var categoryLogger = Mock.Logger("MyApp.Services");

// Generic typed logger (implements ILogger<T>)
var typedLogger = Mock.Logger<MyService>();
Microsoft.Extensions.Logging.ILogger<MyService> typedILogger = typedLogger;
```

## Inspecting Entries

```csharp
logger.LogInformation("User {UserId} logged in", 42);
logger.LogWarning("Disk space low");

// All entries
await Assert.That(logger.Entries).Count().IsEqualTo(2);
await Assert.That(logger.Entries[0].LogLevel).IsEqualTo(Microsoft.Extensions.Logging.LogLevel.Information);
await Assert.That(logger.Entries[0].Message).Contains("42");

// Most recent entry
var latest = logger.LatestEntry;
await Assert.That(latest!.Message).IsEqualTo("Disk space low");
```

Each `LogEntry` provides:

| Property | Description |
|---|---|
| `LogLevel` | The log level (Trace, Debug, Information, etc.) |
| `EventId` | The event ID |
| `Message` | The formatted log message |
| `Exception` | The associated exception (or null) |
| `Timestamp` | When the entry was recorded |
| `CategoryName` | The logger's category name |

## Verification

### Fluent API

Build verification queries with filters:

```csharp
// By level
logger.VerifyLog().AtLevel(Microsoft.Extensions.Logging.LogLevel.Error).WasCalled(Times.Once);

// By message content (contains)
logger.VerifyLog().ContainingMessage("failed").WasCalled(Times.Once);

// By exact message
logger.VerifyLog().WithMessage("Operation completed").WasCalled(Times.Once);

// By exception type
logger.VerifyLog().WithException<InvalidOperationException>().WasCalled(Times.Once);

// Combined filters
logger.VerifyLog()
    .AtLevel(Microsoft.Extensions.Logging.LogLevel.Error)
    .WithException<InvalidOperationException>()
    .ContainingMessage("database")
    .WasCalled(Times.Once);
```

### Shorthand Methods

```csharp
// Verify message at level (at least once)
logger.VerifyLog(Microsoft.Extensions.Logging.LogLevel.Error, "connection failed");

// Verify message at level with count
logger.VerifyLog(Microsoft.Extensions.Logging.LogLevel.Warning, "retry", Times.Exactly(3));

// Verify nothing logged at a level
logger.VerifyNoLog(Microsoft.Extensions.Logging.LogLevel.Error);

// Verify nothing logged at all
logger.VerifyNoLogs();
```

### Never Called

```csharp
logger.VerifyLog().AtLevel(Microsoft.Extensions.Logging.LogLevel.Error).WasNeverCalled();
```

## Filtering Entries

Retrieve entries matching specific criteria:

```csharp
// By level
var errors = logger.GetLogs(Microsoft.Extensions.Logging.LogLevel.Error);

// By message content
var retryLogs = logger.GetLogs("retry");

// Using the fluent API
var matching = logger.VerifyLog()
    .AtLevel(Microsoft.Extensions.Logging.LogLevel.Warning)
    .ContainingMessage("timeout")
    .GetMatchingEntries();
```

## Reset

```csharp
logger.Clear(); // removes all captured entries
```

## Dependency Injection

Pass `Mock.Logger<T>()` anywhere `ILogger<T>` is expected:

```csharp
[Test]
public async Task OrderService_Logs_Errors()
{
    var logger = Mock.Logger<OrderService>();
    var service = new OrderService(logger);

    service.ProcessOrder(invalidOrder);

    logger.VerifyLog()
        .AtLevel(Microsoft.Extensions.Logging.LogLevel.Error)
        .ContainingMessage("validation failed")
        .WasCalled(Times.Once);
}
```
