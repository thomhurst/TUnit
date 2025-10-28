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

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.78.0  | 1.777 s | 0.0332 s | 0.0341 s | 1.765 s |
| Build_NUnit  | 4.4.0   | 1.582 s | 0.0152 s | 0.0143 s | 1.577 s |
| Build_MSTest | 4.0.1   | 1.667 s | 0.0137 s | 0.0128 s | 1.668 s |
| Build_xUnit3 | 3.1.0   | 1.570 s | 0.0101 s | 0.0090 s | 1.569 s |


### Scenario: Tests running asynchronous operations and async/await patterns

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.78.0  | 452.17 ms |  3.388 ms |  2.829 ms | 452.69 ms |
| NUnit     | 4.4.0   | 565.57 ms |  9.621 ms |  8.528 ms | 567.10 ms |
| MSTest    | 4.0.1   | 578.39 ms | 11.254 ms | 12.960 ms | 574.71 ms |
| xUnit3    | 3.1.0   | 522.50 ms |  4.072 ms |  3.610 ms | 522.72 ms |
| TUnit_AOT | 0.78.0  |  24.35 ms |  0.406 ms |  0.380 ms |  24.34 ms |


### Scenario: Parameterized tests with multiple test cases using data attributes

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method    | Version | Mean      | Error    | StdDev    | Median    |
|---------- |-------- |----------:|---------:|----------:|----------:|
| TUnit     | 0.78.0  | 497.97 ms | 9.414 ms | 10.073 ms | 496.53 ms |
| NUnit     | 4.4.0   | 616.29 ms | 8.142 ms |  7.218 ms | 613.56 ms |
| MSTest    | 4.0.1   | 631.89 ms | 8.377 ms |  6.995 ms | 630.92 ms |
| xUnit3    | 3.1.0   | 549.54 ms | 5.447 ms |  4.829 ms | 548.19 ms |
| TUnit_AOT | 0.78.0  |  24.02 ms | 0.201 ms |  0.157 ms |  24.04 ms |


### Scenario: Tests executing massively parallel workloads with CPU-bound, I/O-bound, and mixed operations

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.62GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method    | Version | Mean      | Error     | StdDev   | Median    |
|---------- |-------- |----------:|----------:|---------:|----------:|
| TUnit     | 0.78.0  | 439.78 ms |  3.590 ms | 2.998 ms | 440.24 ms |
| NUnit     | 4.4.0   | 579.16 ms | 10.529 ms | 9.334 ms | 576.13 ms |
| MSTest    | 4.0.1   | 592.15 ms | 11.068 ms | 9.242 ms | 592.37 ms |
| xUnit3    | 3.1.0   | 541.26 ms |  3.818 ms | 3.385 ms | 541.16 ms |
| TUnit_AOT | 0.78.0  |  29.11 ms |  0.289 ms | 0.256 ms |  29.09 ms |


### Scenario: Tests with complex parameter combinations creating 25-125 test variations

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method    | Version | Mean      | Error    | StdDev   | Median    |
|---------- |-------- |----------:|---------:|---------:|----------:|
| TUnit     | 0.78.0  | 477.67 ms | 5.540 ms | 5.182 ms | 477.54 ms |
| NUnit     | 4.4.0   | 603.24 ms | 9.112 ms | 7.609 ms | 603.63 ms |
| MSTest    | 4.0.1   | 618.86 ms | 9.034 ms | 7.544 ms | 618.20 ms |
| xUnit3    | 3.1.0   | 541.77 ms | 4.720 ms | 4.184 ms | 542.61 ms |
| TUnit_AOT | 0.78.0  |  28.99 ms | 0.374 ms | 0.331 ms |  28.99 ms |


### Scenario: Large-scale parameterized tests with 100+ test cases testing framework scalability

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]    : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3
  RyuJitX64 : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v3

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
PowerPlanMode=00000000-0000-0000-0000-000000000000  Runtime=.NET 10.0  Concurrent=True  
Server=True  

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.78.0  | 478.45 ms |  5.465 ms |  4.844 ms | 478.34 ms |
| NUnit     | 4.4.0   | 585.18 ms | 10.662 ms | 10.471 ms | 583.94 ms |
| MSTest    | 4.0.1   | 569.01 ms | 11.041 ms | 15.114 ms | 568.46 ms |
| xUnit3    | 3.1.0   | 508.09 ms |  5.421 ms |  4.806 ms | 508.19 ms |
| TUnit_AOT | 0.78.0  |  46.62 ms |  1.520 ms |  4.481 ms |  47.06 ms |



