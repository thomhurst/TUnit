![](assets/banner.png)

# 🚀 The Modern Testing Framework for .NET

**TUnit** is a modern testing framework for .NET that uses **source-generated tests**, **parallel execution by default**, and **Native AOT support**. Built on Microsoft.Testing.Platform, it's faster than traditional reflection-based frameworks and gives you more control over how your tests run.

<div align="center">

[![thomhurst%2FTUnit | Trendshift](https://trendshift.io/api/badge/repositories/11781)](https://trendshift.io/repositories/11781)


[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a8231644d844435eb9fd15110ea771d8)](https://app.codacy.com/gh/thomhurst/TUnit?utm_source=github.com&utm_medium=referral&utm_content=thomhurst/TUnit&utm_campaign=Badge_Grade)![GitHub Repo stars](https://img.shields.io/github/stars/thomhurst/TUnit) ![GitHub Issues or Pull Requests](https://img.shields.io/github/issues-closed-raw/thomhurst/TUnit)
 [![GitHub Sponsors](https://img.shields.io/github/sponsors/thomhurst)](https://github.com/sponsors/thomhurst) [![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) [![NuGet Downloads](https://img.shields.io/nuget/dt/TUnit)](https://www.nuget.org/packages/TUnit/) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit)

</div>

## Why TUnit?

| Feature | Traditional Frameworks | **TUnit** |
|---------|----------------------|-----------|
| Test Discovery | ❌ Runtime reflection | ✅ **Compile-time generation** |
| Execution Speed | ❌ Sequential by default | ✅ **Parallel by default** |
| Modern .NET | ⚠️ Limited AOT support | ✅ **Native AOT & trimming** |
| Test Dependencies | ❌ Not supported | ✅ **`[DependsOn]` chains** |
| Resource Management | ❌ Manual lifecycle | ✅ **Automatic cleanup** |

**Parallel by Default** - Tests run concurrently with dependency management

**Compile-Time Discovery** - Test structure is known before runtime

**Modern .NET Ready** - Native AOT, trimming, and latest .NET features

**Extensible** - Customize data sources, attributes, and test behavior

---

<div align="center">

## **[Documentation](https://tunit.dev)**

**New to TUnit?** Start with the **[Getting Started Guide](https://tunit.dev/docs/getting-started/installation)**

**Migrating?** See the **[Migration Guides](https://tunit.dev/docs/migration/xunit)**

**Learn more:** **[Data-Driven Testing](https://tunit.dev/docs/test-authoring/arguments)**, **[Test Dependencies](https://tunit.dev/docs/test-authoring/depends-on)**, **[Parallelism Control](https://tunit.dev/docs/parallelism/not-in-parallel)**

</div>

---

## Quick Start

### Using the Project Template (Recommended)
```bash
dotnet new install TUnit.Templates
dotnet new TUnit -n "MyTestProject"
```

### Manual Installation
```bash
dotnet add package TUnit --prerelease
```

📖 **[Complete Documentation & Guides](https://tunit.dev)**

## Key Features

<table>
<tr>
<td width="50%">

**Performance**
- Source-generated tests (no reflection)
- Parallel execution by default
- Native AOT & trimming support
- Optimized for speed

</td>
<td width="50%">

**Test Control**
- Test dependencies with `[DependsOn]`
- Parallel limits & custom scheduling
- Built-in analyzers & compile-time checks
- Custom attributes & extensible conditions

</td>
</tr>
<tr>
<td>

**Data & Assertions**
- Multiple data sources (`[Arguments]`, `[Matrix]`, `[ClassData]`)
- Fluent async assertions
- Retry logic & conditional execution
- Test metadata & context

</td>
<td>

**Developer Tools**
- Full dependency injection support
- Lifecycle hooks
- IDE integration (VS, Rider, VS Code)
- Documentation & examples

</td>
</tr>
</table>

## Simple Test Example

```csharp
[Test]
public async Task User_Creation_Should_Set_Timestamp()
{
    // Arrange
    var userService = new UserService();

    // Act
    var user = await userService.CreateUserAsync("john.doe@example.com");

    // Assert - TUnit's fluent assertions
    await Assert.That(user.CreatedAt)
        .IsEqualTo(DateTime.Now)
        .Within(TimeSpan.FromMinutes(1));

    await Assert.That(user.Email)
        .IsEqualTo("john.doe@example.com");
}
```

## Data-Driven Testing

```csharp
[Test]
[Arguments("user1@test.com", "ValidPassword123")]
[Arguments("user2@test.com", "AnotherPassword456")]
[Arguments("admin@test.com", "AdminPass789")]
public async Task User_Login_Should_Succeed(string email, string password)
{
    var result = await authService.LoginAsync(email, password);
    await Assert.That(result.IsSuccess).IsTrue();
}

// Matrix testing - tests all combinations
[Test]
[MatrixDataSource]
public async Task Database_Operations_Work(
    [Matrix("Create", "Update", "Delete")] string operation,
    [Matrix("User", "Product", "Order")] string entity)
{
    await Assert.That(await ExecuteOperation(operation, entity))
        .IsTrue();
}
```

## Advanced Test Orchestration

```csharp
[Before(Class)]
public static async Task SetupDatabase(ClassHookContext context)
{
    await DatabaseHelper.InitializeAsync();
}

[Test, DisplayName("Register a new account")]
[MethodDataSource(nameof(GetTestUsers))]
public async Task Register_User(string username, string password)
{
    // Test implementation
}

[Test, DependsOn(nameof(Register_User))]
[Retry(3)] // Retry on failure
public async Task Login_With_Registered_User(string username, string password)
{
    // This test runs after Register_User completes
}

[Test]
[ParallelLimit<LoadTestParallelLimit>] // Custom parallel control
[Repeat(100)] // Run 100 times
public async Task Load_Test_Homepage()
{
    // Performance testing
}

// Custom attributes
[Test, WindowsOnly, RetryOnHttpError(5)]
public async Task Windows_Specific_Feature()
{
    // Platform-specific test with custom retry logic
}

public class LoadTestParallelLimit : IParallelLimit
{
    public int Limit => 10; // Limit to 10 concurrent executions
}
```

## Custom Test Control

```csharp
// Custom conditional execution
public class WindowsOnlyAttribute : SkipAttribute
{
    public WindowsOnlyAttribute() : base("Windows only test") { }

    public override Task<bool> ShouldSkip(TestContext testContext)
        => Task.FromResult(!OperatingSystem.IsWindows());
}

// Custom retry logic
public class RetryOnHttpErrorAttribute : RetryAttribute
{
    public RetryOnHttpErrorAttribute(int times) : base(times) { }

    public override Task<bool> ShouldRetry(TestInformation testInformation,
        Exception exception, int currentRetryCount)
        => Task.FromResult(exception is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable });
}
```

## Common Use Cases

<table>
<tr>
<td width="33%">

### **Unit Testing**
```csharp
[Test]
[Arguments(1, 2, 3)]
[Arguments(5, 10, 15)]
public async Task Calculate_Sum(int a, int b, int expected)
{
    await Assert.That(Calculator.Add(a, b))
        .IsEqualTo(expected);
}
```

</td>
<td width="33%">

### **Integration Testing**
```csharp
[Test, DependsOn(nameof(CreateUser))]
public async Task Login_After_Registration()
{
    // Runs after CreateUser completes
    var result = await authService.Login(user);
    await Assert.That(result.IsSuccess).IsTrue();
}
```

</td>
<td width="33%">

### **Load Testing**
```csharp
[Test]
[ParallelLimit<LoadTestLimit>]
[Repeat(1000)]
public async Task API_Handles_Concurrent_Requests()
{
    await Assert.That(await httpClient.GetAsync("/api/health"))
        .HasStatusCode(HttpStatusCode.OK);
}
```

</td>
</tr>
</table>

## What Makes TUnit Different?

### **Compile-Time Test Discovery**
Tests are discovered at build time, not runtime. This means faster discovery, better IDE integration, and more predictable resource management.

### **Parallel by Default**
Tests run in parallel by default. Use `[DependsOn]` to chain tests together, and `[ParallelLimit]` to control resource usage.

### **Extensible**
The `DataSourceGenerator<T>` pattern and custom attribute system let you extend TUnit without modifying the framework.

## Community & Ecosystem

<div align="center">

[![Downloads](https://img.shields.io/nuget/dt/TUnit?label=Downloads&color=blue)](https://www.nuget.org/packages/TUnit/)
[![Contributors](https://img.shields.io/github/contributors/thomhurst/TUnit?label=Contributors)](https://github.com/thomhurst/TUnit/graphs/contributors)
[![Discussions](https://img.shields.io/github/discussions/thomhurst/TUnit?label=Discussions)](https://github.com/thomhurst/TUnit/discussions)

</div>

### **Resources**
- **[Official Documentation](https://tunit.dev)** - Guides, tutorials, and API reference
- **[GitHub Discussions](https://github.com/thomhurst/TUnit/discussions)** - Get help and share ideas
- **[Issue Tracking](https://github.com/thomhurst/TUnit/issues)** - Report bugs and request features
- **[Release Notes](https://github.com/thomhurst/TUnit/releases)** - Latest updates and changes

## IDE Support

TUnit works with all major .NET IDEs:

### Visual Studio (2022 17.13+)
✅ **Fully supported** - No additional configuration needed for latest versions

⚙️ **Earlier versions**: Enable "Use testing platform server mode" in Tools > Manage Preview Features

### JetBrains Rider
✅ **Fully supported**

⚙️ **Setup**: Enable "Testing Platform support" in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform

### Visual Studio Code
✅ **Fully supported**

⚙️ **Setup**: Install [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) and enable "Use Testing Platform Protocol"

### Command Line
✅ **Full CLI support** - Works with `dotnet test`, `dotnet run`, and direct executable execution

## Package Options

| Package | Use Case |
|---------|----------|
| **`TUnit`** | **Start here** - Complete testing framework (includes Core + Engine + Assertions) |
| **`TUnit.Core`** | Test libraries and shared components (no execution engine) |
| **`TUnit.Engine`** | Test execution engine and adapter (for test projects) |
| **`TUnit.Assertions`** | Standalone assertions (works with any test framework) |
| **`TUnit.Playwright`** | Playwright integration with automatic lifecycle management |

## Migration from Other Frameworks

**Coming from NUnit or xUnit?** TUnit uses familiar syntax with some additions:

```csharp
// TUnit test with dependency management and retries
[Test]
[Arguments("value1")]
[Arguments("value2")]
[Retry(3)]
[ParallelLimit<CustomLimit>]
public async Task Modern_TUnit_Test(string value) { }
```

📖 **Need help migrating?** Check our **[Migration Guides](https://tunit.dev/docs/migration/xunit)** for xUnit, NUnit, and MSTest.


## Current Status

The API is mostly stable, but may have some changes based on feedback before the v1.0 release.

---

<div align="center">

## Getting Started

```bash
# Create a new test project
dotnet new install TUnit.Templates && dotnet new TUnit -n "MyTestProject"

# Or add to existing project
dotnet add package TUnit --prerelease
```

**Learn More**: [tunit.dev](https://tunit.dev) | **Get Help**: [GitHub Discussions](https://github.com/thomhurst/TUnit/discussions) | **Star on GitHub**: [github.com/thomhurst/TUnit](https://github.com/thomhurst/TUnit)

</div>

## Performance Benchmark

### Scenario: Building the test project

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.90.35 | 1.839 s | 0.0360 s | 0.0300 s | 1.842 s |
| Build_NUnit  | 4.4.0   | 1.608 s | 0.0175 s | 0.0155 s | 1.605 s |
| Build_MSTest | 4.0.1   | 1.694 s | 0.0274 s | 0.0256 s | 1.691 s |
| Build_xUnit3 | 3.2.0   | 1.572 s | 0.0138 s | 0.0129 s | 1.570 s |


### Scenario: Tests running asynchronous operations and async/await patterns

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0  

```
| Method    | Version | Mean     | Error    | StdDev   | Median   |
|---------- |-------- |---------:|---------:|---------:|---------:|
| TUnit     | 0.90.35 | 557.3 ms |  6.65 ms |  6.22 ms | 556.4 ms |
| NUnit     | 4.4.0   | 704.9 ms |  8.24 ms |  7.30 ms | 704.8 ms |
| MSTest    | 4.0.1   | 685.8 ms | 10.89 ms | 10.19 ms | 684.6 ms |
| xUnit3    | 3.2.0   | 745.3 ms |  4.31 ms |  3.60 ms | 746.1 ms |
| TUnit_AOT | 0.90.35 | 123.8 ms |  0.43 ms |  0.41 ms | 123.8 ms |


### Scenario: Parameterized tests with multiple test cases using data attributes

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4

Runtime=.NET 10.0  

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.90.35 | 467.49 ms |  7.861 ms |  7.353 ms | 467.53 ms |
| NUnit     | 4.4.0   | 585.62 ms | 11.351 ms | 15.912 ms | 587.11 ms |
| MSTest    | 4.0.1   | 601.89 ms | 11.992 ms | 15.166 ms | 598.20 ms |
| xUnit3    | 3.2.0   | 601.70 ms | 11.454 ms | 10.154 ms | 603.46 ms |
| TUnit_AOT | 0.90.35 |  25.00 ms |  0.471 ms |  0.440 ms |  25.00 ms |


### Scenario: Tests executing massively parallel workloads with CPU-bound, I/O-bound, and mixed operations

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0  

```
| Method    | Version | Mean       | Error    | StdDev   | Median     |
|---------- |-------- |-----------:|---------:|---------:|-----------:|
| TUnit     | 0.90.35 | 1,124.7 ms |  4.39 ms |  4.10 ms | 1,124.3 ms |
| NUnit     | 4.4.0   | 1,200.1 ms |  7.11 ms |  6.65 ms | 1,200.7 ms |
| MSTest    | 4.0.1   | 2,980.7 ms |  5.86 ms |  5.48 ms | 2,979.7 ms |
| xUnit3    | 3.2.0   | 3,076.6 ms | 10.92 ms | 10.21 ms | 3,076.4 ms |
| TUnit_AOT | 0.90.35 |   682.5 ms |  1.92 ms |  1.70 ms |   682.9 ms |


### Scenario: Tests with complex parameter combinations creating 25-125 test variations

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0  

```
| Method    | Version | Mean       | Error    | StdDev   | Median     |
|---------- |-------- |-----------:|---------:|---------:|-----------:|
| TUnit     | 0.90.35 |   752.7 ms | 11.03 ms | 10.32 ms |   751.7 ms |
| NUnit     | 4.4.0   | 1,571.0 ms |  6.84 ms |  6.06 ms | 1,572.5 ms |
| MSTest    | 4.0.1   | 1,521.7 ms | 11.63 ms | 10.88 ms | 1,519.7 ms |
| xUnit3    | 3.2.0   | 1,598.8 ms |  9.76 ms |  9.13 ms | 1,597.2 ms |
| TUnit_AOT | 0.90.35 |   281.0 ms |  0.71 ms |  0.63 ms |   281.1 ms |


### Scenario: Large-scale parameterized tests with 100+ test cases testing framework scalability

```

BenchmarkDotNet v0.15.5, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0  

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.90.35 | 490.55 ms |  5.567 ms |  4.648 ms | 491.24 ms |
| NUnit     | 4.4.0   | 603.45 ms | 11.699 ms | 11.490 ms | 601.75 ms |
| MSTest    | 4.0.1   | 608.58 ms | 10.237 ms |  9.075 ms | 610.85 ms |
| xUnit3    | 3.2.0   | 606.27 ms |  8.832 ms |  7.829 ms | 605.27 ms |
| TUnit_AOT | 0.90.35 |  54.50 ms |  1.076 ms |  2.758 ms |  54.75 ms |



