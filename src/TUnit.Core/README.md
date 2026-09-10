# TUnit.Core

A modern, fast, and flexible .NET testing framework with the revolutionary TestBuilder architecture.

## Features

- 🚀 **High Performance**: Optimized test discovery and execution
- 🔧 **TestBuilder Architecture**: Maintainable, debuggable test generation
- 🎯 **Source Generated**: Compile-time test discovery, no reflection overhead
- 🔄 **Parallel by Default**: Tests run in parallel for faster execution
- 📊 **Rich Telemetry**: Built-in metrics and diagnostics
- 🛡️ **Type Safe**: Full AOT and trimming support
- 🔌 **Extensible**: Custom data sources, hooks, and assertions

## Quick Start

### 1. Install the Package

```bash
dotnet add package TUnit.Core
```

### 2. Write Your First Test

```csharp
using TUnit.Core;

public class CalculatorTests
{
    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        // Arrange
        var calculator = new Calculator();
        
        // Act
        var result = calculator.Add(2, 3);
        
        // Assert
        Assert.That(result).IsEqualTo(5);
    }
}
```

### 3. Run Tests

```bash
dotnet test
```

## TestBuilder Architecture

TUnit uses the TestBuilder architecture, with complex test execution logic at runtime for better maintainability and performance. The source generator emits only test metadata, while the runtime TestBuilder handles all execution details.

### Benefits

- **Better Performance**: Optimized with caching and expression compilation
- **Enhanced Debugging**: Step through actual code, not generated strings
- **Improved Errors**: Clear, actionable error messages
- **Easy Extension**: Add custom behaviors without modifying source generator

## Data-Driven Tests

```csharp
[Test]
[Arguments(1, 1, 2)]
[Arguments(2, 3, 5)]
[Arguments(-1, 1, 0)]
public void Add_WithTestCases_ReturnsExpectedSum(int a, int b, int expected)
{
    var result = Calculator.Add(a, b);
    Assert.That(result).IsEqualTo(expected);
}

[Test]
[MethodDataSource(nameof(GetTestData))]
public void Add_WithMethodData_Works(int a, int b, int expected)
{
    var result = Calculator.Add(a, b);
    Assert.That(result).IsEqualTo(expected);
}

public static IEnumerable<(int, int, int)> GetTestData()
{
    yield return (1, 1, 2);
    yield return (2, 3, 5);
    yield return (-1, 1, 0);
}
```

## Async Tests

```csharp
[Test]
public async Task GetDataAsync_ReturnsExpectedValue()
{
    var service = new DataService();
    var result = await service.GetDataAsync();
    
    await Assert.That(result).IsNotNull();
    await Assert.That(result.Count).IsGreaterThan(0);
}
```

## Test Lifecycle Hooks

```csharp
public class DatabaseTests
{
    private TestDatabase _database;
    
    [Before(HookType.Test)]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
    }
    
    [After(HookType.Test)]
    public async Task TearDown()
    {
        await _database.DisposeAsync();
    }
    
    [Test]
    public async Task Database_CanSaveAndRetrieve()
    {
        // Use _database in test
    }
}
```

## Configuration

### MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `TUnitMaxConcurrency` | `ProcessorCount` | Maximum parallel test execution |

## Advanced Features

### Custom Data Sources

```csharp
public class DatabaseDataSource : IDataSourceProvider
{
    public IEnumerable<object[]> GetData()
    {
        using var db = new TestDatabase();
        return db.GetTestCases();
    }
}

[Test]
[DataSource(typeof(DatabaseDataSource))]
public void TestWithDatabaseData(string input, string expected)
{
    // Test implementation
}
```

### Test Dependencies

```csharp
[Test]
[DependsOn(nameof(TestA))]
public void TestB()
{
    // This test runs after TestA
}
```

### Conditional Tests

```csharp
[Test]
[SkipIf(nameof(IsCI), "Skipped in CI environment")]
public void LocalOnlyTest()
{
    // Test that only runs locally
}

public static bool IsCI => Environment.GetEnvironmentVariable("CI") == "true";
```

## Diagnostics and Troubleshooting

### Debugging Tests

TUnit's simplified architecture makes debugging straightforward. You can set breakpoints in your test code and step through the execution flow.

## Migration from Other Frameworks

### From xUnit

```csharp
// xUnit
[Fact]
public void Test() { }

// TUnit
[Test]
public void Test() { }

// xUnit
[Theory]
[InlineData(1, 2, 3)]
public void Test(int a, int b, int c) { }

// TUnit
[Test]
[Arguments(1, 2, 3)]
public void Test(int a, int b, int c) { }
```

### From NUnit

```csharp
// NUnit
[TestCase(1, 2, 3)]
public void Test(int a, int b, int c) { }

// TUnit
[Test]
[Arguments(1, 2, 3)]
public void Test(int a, int b, int c) { }
```

## Performance Tips

1. **Expression Compilation**: TestBuilder uses compiled expressions for fast execution
2. **Share Data Sources**: Use `Shared = true` for expensive data
3. **Optimize Parallel Execution**: Adjust `TUnitMaxConcurrency`
4. **Cache Test Data**: Use lazy initialization for expensive setup

## Contributing

See the [contribution guide](https://github.com/thomhurst/TUnit/blob/main/CONTRIBUTING.md) for details.

## License

MIT License - see [LICENSE](https://github.com/thomhurst/TUnit/blob/main/LICENSE) for details.

## Support

- 📖 [Documentation](https://github.com/thomhurst/TUnit/wiki)
- 💬 [Discussions](https://github.com/thomhurst/TUnit/discussions)
- 🐛 [Issues](https://github.com/thomhurst/TUnit/issues)
- 📧 [Email](mailto:tom@longhurst.dev)