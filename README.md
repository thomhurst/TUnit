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

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.66.6  | 1.425 s | 0.0954 s | 0.2722 s | 1.384 s |
| Build_NUnit  | 4.4.0   | 1.171 s | 0.0670 s | 0.1934 s | 1.150 s |
| Build_xUnit  | 2.9.3   | 1.149 s | 0.0853 s | 0.2448 s | 1.101 s |
| Build_MSTest | 3.11.0  | 1.108 s | 0.0468 s | 0.1365 s | 1.103 s |
| Build_xUnit3 | 3.1.0   | 1.144 s | 0.0763 s | 0.2238 s | 1.093 s |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.66.6  | 1.645 s | 0.0212 s | 0.0188 s | 1.645 s |
| Build_NUnit  | 4.4.0   | 1.500 s | 0.0294 s | 0.0302 s | 1.502 s |
| Build_xUnit  | 2.9.3   | 1.513 s | 0.0175 s | 0.0163 s | 1.513 s |
| Build_MSTest | 3.11.0  | 1.534 s | 0.0136 s | 0.0127 s | 1.539 s |
| Build_xUnit3 | 3.1.0   | 1.496 s | 0.0247 s | 0.0219 s | 1.500 s |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.66.6  | 2.134 s | 0.0440 s | 0.1275 s | 2.143 s |
| Build_NUnit  | 4.4.0   | 2.085 s | 0.0360 s | 0.0319 s | 2.085 s |
| Build_xUnit  | 2.9.3   | 2.055 s | 0.0406 s | 0.0865 s | 2.057 s |
| Build_MSTest | 3.11.0  | 2.160 s | 0.0431 s | 0.1142 s | 2.152 s |
| Build_xUnit3 | 3.1.0   | 2.136 s | 0.0424 s | 0.0505 s | 2.124 s |


### Scenario: Tests focused on assertion performance and validation

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 507.19 ms | 28.541 ms |  83.71 ms | 497.91 ms |
| NUnit     | 4.4.0   |        NA |        NA |        NA |        NA |
| xUnit     | 2.9.3   | 884.34 ms | 54.651 ms | 160.28 ms | 895.71 ms |
| MSTest    | 3.11.0  | 768.27 ms | 39.207 ms | 115.60 ms | 746.33 ms |
| xUnit3    | 3.1.0   | 493.87 ms | 16.161 ms |  47.40 ms | 503.28 ms |
| TUnit_AOT | 0.66.6  |  70.85 ms |  7.276 ms |  21.34 ms |  67.87 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 487.37 ms |  3.093 ms |  2.893 ms | 487.43 ms |
| NUnit     | 4.4.0   | 908.34 ms | 17.276 ms | 16.967 ms | 911.94 ms |
| xUnit     | 2.9.3   | 984.77 ms | 19.259 ms | 18.015 ms | 979.06 ms |
| MSTest    | 3.11.0  | 836.95 ms | 11.507 ms | 10.764 ms | 832.08 ms |
| xUnit3    | 3.1.0   | 458.16 ms |  4.159 ms |  3.890 ms | 457.83 ms |
| TUnit_AOT | 0.66.6  |  25.31 ms |  0.431 ms |  0.382 ms |  25.16 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   533.36 ms |  3.679 ms |  3.441 ms |   532.96 ms |
| NUnit     | 4.4.0   | 1,016.18 ms | 20.186 ms | 34.820 ms | 1,015.43 ms |
| xUnit     | 2.9.3   | 1,068.97 ms | 20.224 ms | 16.888 ms | 1,073.74 ms |
| MSTest    | 3.11.0  |   968.58 ms | 17.772 ms | 31.127 ms |   964.24 ms |
| xUnit3    | 3.1.0   |   516.29 ms |  7.819 ms |  6.931 ms |   515.39 ms |
| TUnit_AOT | 0.66.6  |    65.97 ms |  2.281 ms |  6.724 ms |    65.43 ms |


### Scenario: Tests running asynchronous operations and async/await patterns

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit     | 0.66.6  |   608.9 ms | 39.85 ms | 117.49 ms |   577.8 ms |
| NUnit     | 4.4.0   | 1,260.3 ms | 57.30 ms | 166.24 ms | 1,274.6 ms |
| xUnit     | 2.9.3   |         NA |       NA |        NA |         NA |
| MSTest    | 3.11.0  | 1,050.5 ms | 41.59 ms | 119.99 ms | 1,053.4 ms |
| xUnit3    | 3.1.0   |   559.6 ms | 21.67 ms |  62.18 ms |   567.1 ms |
| TUnit_AOT | 0.66.6  |   144.6 ms | 11.89 ms |  34.88 ms |   145.0 ms |

Benchmarks with issues:
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 447.34 ms |  3.704 ms |  3.465 ms | 447.86 ms |
| NUnit     | 4.4.0   | 907.30 ms | 17.922 ms | 18.404 ms | 901.36 ms |
| xUnit     | 2.9.3   | 988.70 ms | 19.061 ms | 17.830 ms | 983.11 ms |
| MSTest    | 3.11.0  | 842.79 ms | 16.374 ms | 17.520 ms | 842.60 ms |
| xUnit3    | 3.1.0   | 460.86 ms |  2.968 ms |  2.777 ms | 459.81 ms |
| TUnit_AOT | 0.66.6  |  26.67 ms |  0.506 ms |  0.562 ms |  26.78 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   494.36 ms |  7.194 ms |  6.729 ms |   490.64 ms |
| NUnit     | 4.4.0   | 1,005.78 ms | 18.596 ms | 17.394 ms | 1,007.02 ms |
| xUnit     | 2.9.3   | 1,096.34 ms | 20.797 ms | 22.253 ms | 1,097.98 ms |
| MSTest    | 3.11.0  |   962.06 ms | 18.672 ms | 26.175 ms |   957.03 ms |
| xUnit3    | 3.1.0   |   522.65 ms |  9.988 ms |  9.343 ms |   521.69 ms |
| TUnit_AOT | 0.66.6  |    89.31 ms |  2.294 ms |  6.728 ms |    89.53 ms |


### Scenario: Simple tests with basic operations and assertions

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev   | Median    |
|---------- |-------- |----------:|----------:|---------:|----------:|
| TUnit     | 0.66.6  | 413.11 ms | 29.400 ms | 85.30 ms | 389.28 ms |
| NUnit     | 4.4.0   |        NA |        NA |       NA |        NA |
| xUnit     | 2.9.3   |        NA |        NA |       NA |        NA |
| MSTest    | 3.11.0  |        NA |        NA |       NA |        NA |
| xUnit3    | 3.1.0   | 289.63 ms |  7.455 ms | 21.51 ms | 285.72 ms |
| TUnit_AOT | 0.66.6  |  66.85 ms |  9.402 ms | 27.57 ms |  61.87 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 466.81 ms |  5.399 ms |  4.786 ms | 466.44 ms |
| NUnit     | 4.4.0   | 919.54 ms | 16.718 ms | 15.638 ms | 919.80 ms |
| xUnit     | 2.9.3   | 994.05 ms | 10.533 ms |  9.337 ms | 992.55 ms |
| MSTest    | 3.11.0  | 853.21 ms | 16.534 ms | 17.691 ms | 854.80 ms |
| xUnit3    | 3.1.0   | 459.93 ms |  3.301 ms |  2.926 ms | 458.96 ms |
| TUnit_AOT | 0.66.6  |  25.34 ms |  0.444 ms |  0.415 ms |  25.14 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   517.27 ms |  4.416 ms |  4.130 ms |   518.39 ms |
| NUnit     | 4.4.0   | 1,018.48 ms | 20.084 ms | 19.725 ms | 1,014.70 ms |
| xUnit     | 2.9.3   | 1,079.55 ms | 19.399 ms | 18.145 ms | 1,079.08 ms |
| MSTest    | 3.11.0  |   950.05 ms | 18.521 ms | 21.329 ms |   953.91 ms |
| xUnit3    | 3.1.0   |   509.16 ms |  6.805 ms |  6.365 ms |   507.00 ms |
| TUnit_AOT | 0.66.6  |    66.58 ms |  1.505 ms |  4.414 ms |    65.67 ms |


### Scenario: Parameterized tests with multiple test cases using data attributes

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev   | Median    |
|---------- |-------- |----------:|----------:|---------:|----------:|
| TUnit     | 0.66.6  | 408.74 ms | 18.928 ms | 55.21 ms | 396.30 ms |
| NUnit     | 4.4.0   |        NA |        NA |       NA |        NA |
| xUnit     | 2.9.3   |        NA |        NA |       NA |        NA |
| MSTest    | 3.11.0  | 659.03 ms | 22.634 ms | 66.38 ms | 666.53 ms |
| xUnit3    | 3.1.0   | 377.11 ms | 11.684 ms | 33.90 ms | 379.14 ms |
| TUnit_AOT | 0.66.6  |  44.69 ms |  3.968 ms | 11.39 ms |  41.36 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   460.22 ms |  2.117 ms |  1.877 ms |   460.03 ms |
| NUnit     | 4.4.0   |   909.38 ms | 16.413 ms | 15.352 ms |   917.73 ms |
| xUnit     | 2.9.3   | 1,016.83 ms | 18.472 ms | 17.279 ms | 1,008.66 ms |
| MSTest    | 3.11.0  |   866.29 ms | 16.852 ms | 18.031 ms |   865.01 ms |
| xUnit3    | 3.1.0   |   486.65 ms |  3.559 ms |  3.155 ms |   487.03 ms |
| TUnit_AOT | 0.66.6  |    28.22 ms |  0.343 ms |  0.304 ms |    28.31 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev     | Median      |
|---------- |-------- |------------:|----------:|-----------:|------------:|
| TUnit     | 0.66.6  |   550.13 ms | 10.668 ms |  10.477 ms |   545.52 ms |
| NUnit     | 4.4.0   | 1,115.22 ms | 22.111 ms |  23.658 ms | 1,115.33 ms |
| xUnit     | 2.9.3   | 1,209.43 ms | 35.495 ms | 100.694 ms | 1,223.99 ms |
| MSTest    | 3.11.0  | 1,041.10 ms | 20.681 ms |  23.817 ms | 1,038.52 ms |
| xUnit3    | 3.1.0   |   565.34 ms | 10.258 ms |   9.596 ms |   565.75 ms |
| TUnit_AOT | 0.66.6  |    71.69 ms |  1.423 ms |   3.410 ms |    71.39 ms |


### Scenario: Tests utilizing class fixtures and shared test context

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev   | Median    |
|---------- |-------- |----------:|----------:|---------:|----------:|
| TUnit     | 0.66.6  | 286.49 ms |  6.152 ms | 17.75 ms | 285.41 ms |
| NUnit     | 4.4.0   |        NA |        NA |       NA |        NA |
| xUnit     | 2.9.3   |        NA |        NA |       NA |        NA |
| MSTest    | 3.11.0  |        NA |        NA |       NA |        NA |
| xUnit3    | 3.1.0   | 375.36 ms | 22.387 ms | 66.01 ms | 362.13 ms |
| TUnit_AOT | 0.66.6  |  64.68 ms |  6.634 ms | 19.03 ms |  62.08 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   452.99 ms |  3.907 ms |  3.263 ms |   453.44 ms |
| NUnit     | 4.4.0   |   930.95 ms | 14.776 ms | 13.821 ms |   935.97 ms |
| xUnit     | 2.9.3   | 1,006.26 ms | 16.690 ms | 15.611 ms | 1,000.82 ms |
| MSTest    | 3.11.0  |   850.14 ms | 16.934 ms | 18.119 ms |   852.12 ms |
| xUnit3    | 3.1.0   |   450.19 ms |  4.025 ms |  3.765 ms |   450.09 ms |
| TUnit_AOT | 0.66.6  |    38.57 ms |  1.098 ms |  3.220 ms |    38.60 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   495.87 ms |  8.424 ms | 10.653 ms |   492.34 ms |
| NUnit     | 4.4.0   |   973.98 ms | 19.011 ms | 18.671 ms |   972.03 ms |
| xUnit     | 2.9.3   | 1,049.78 ms | 14.531 ms | 13.593 ms | 1,044.49 ms |
| MSTest    | 3.11.0  |   916.98 ms | 16.913 ms | 15.821 ms |   912.23 ms |
| xUnit3    | 3.1.0   |   486.78 ms |  3.897 ms |  3.645 ms |   486.01 ms |
| TUnit_AOT | 0.66.6  |    91.20 ms |  1.800 ms |  3.152 ms |    91.43 ms |


### Scenario: Tests executing in parallel to test framework parallelization

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error      | StdDev    | Median      |
|---------- |-------- |------------:|-----------:|----------:|------------:|
| TUnit     | 0.66.6  |   410.22 ms |  23.733 ms |  69.23 ms |   402.84 ms |
| NUnit     | 4.4.0   | 1,342.07 ms |  95.053 ms | 274.25 ms | 1,303.84 ms |
| xUnit     | 2.9.3   | 1,328.93 ms | 101.864 ms | 300.35 ms | 1,301.91 ms |
| MSTest    | 3.11.0  |          NA |         NA |        NA |          NA |
| xUnit3    | 3.1.0   |   470.25 ms |  20.347 ms |  58.38 ms |   468.24 ms |
| TUnit_AOT | 0.66.6  |    58.77 ms |   7.644 ms |  21.93 ms |    53.57 ms |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 442.19 ms |  1.731 ms |  1.534 ms | 441.97 ms |
| NUnit     | 4.4.0   | 918.06 ms | 17.680 ms | 17.364 ms | 921.88 ms |
| xUnit     | 2.9.3   | 983.91 ms | 18.873 ms | 17.654 ms | 979.36 ms |
| MSTest    | 3.11.0  | 838.79 ms | 14.777 ms | 13.823 ms | 844.27 ms |
| xUnit3    | 3.1.0   | 449.58 ms |  2.903 ms |  2.573 ms | 449.82 ms |
| TUnit_AOT | 0.66.6  |  24.97 ms |  0.208 ms |  0.184 ms |  24.95 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   510.80 ms |  3.865 ms |  3.616 ms |   511.67 ms |
| NUnit     | 4.4.0   | 1,085.21 ms | 21.543 ms | 37.161 ms | 1,079.30 ms |
| xUnit     | 2.9.3   | 1,125.35 ms | 22.326 ms | 27.418 ms | 1,124.00 ms |
| MSTest    | 3.11.0  |   979.31 ms | 15.894 ms | 14.867 ms |   979.73 ms |
| xUnit3    | 3.1.0   |   526.16 ms |  5.769 ms |  5.396 ms |   526.42 ms |
| TUnit_AOT | 0.66.6  |    65.78 ms |  1.726 ms |  5.063 ms |    65.93 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean      | Error     | StdDev    | Median    |
|---------- |-------- |----------:|----------:|----------:|----------:|
| TUnit     | 0.66.6  | 338.92 ms | 19.812 ms | 58.105 ms | 325.53 ms |
| NUnit     | 4.4.0   | 574.90 ms |  9.258 ms |  7.228 ms | 572.83 ms |
| xUnit     | 2.9.3   |        NA |        NA |        NA |        NA |
| MSTest    | 3.11.0  |        NA |        NA |        NA |        NA |
| xUnit3    | 3.1.0   | 324.91 ms | 10.115 ms | 29.507 ms | 318.54 ms |
| TUnit_AOT | 0.66.6  |  60.53 ms |  8.531 ms | 24.613 ms |  56.15 ms |

Benchmarks with issues:
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   486.94 ms |  3.897 ms |  3.645 ms |   487.44 ms |
| NUnit     | 4.4.0   |   925.61 ms | 17.307 ms | 16.189 ms |   922.59 ms |
| xUnit     | 2.9.3   | 1,085.95 ms | 20.200 ms | 17.906 ms | 1,082.26 ms |
| MSTest    | 3.11.0  |   858.03 ms | 16.000 ms | 14.966 ms |   856.06 ms |
| xUnit3    | 3.1.0   |   491.88 ms |  4.490 ms |  3.980 ms |   489.95 ms |
| TUnit_AOT | 0.66.6  |    40.90 ms |  0.680 ms |  0.636 ms |    40.67 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   560.34 ms | 10.543 ms |  9.862 ms |   559.08 ms |
| NUnit     | 4.4.0   | 1,072.50 ms | 20.980 ms | 45.608 ms | 1,074.71 ms |
| xUnit     | 2.9.3   | 1,232.15 ms | 24.387 ms | 26.094 ms | 1,232.77 ms |
| MSTest    | 3.11.0  |   994.43 ms | 19.847 ms | 54.995 ms |   991.14 ms |
| xUnit3    | 3.1.0   |   580.80 ms | 11.371 ms | 15.180 ms |   581.16 ms |
| TUnit_AOT | 0.66.6  |    81.43 ms |  2.104 ms |  6.203 ms |    82.90 ms |


### Scenario: Tests with setup and teardown lifecycle methods

#### macos-latest

```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   525.73 ms | 37.455 ms | 108.66 ms |   518.72 ms |
| NUnit     | 4.4.0   | 1,163.42 ms | 50.082 ms | 143.69 ms | 1,154.49 ms |
| xUnit     | 2.9.3   | 1,063.35 ms | 38.811 ms | 113.83 ms | 1,063.37 ms |
| MSTest    | 3.11.0  |   864.08 ms | 79.507 ms | 234.43 ms |   768.70 ms |
| xUnit3    | 3.1.0   |   389.16 ms | 18.371 ms |  53.59 ms |   391.37 ms |
| TUnit_AOT | 0.66.6  |    50.16 ms |  6.305 ms |  18.19 ms |    48.60 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   467.37 ms |  2.088 ms |  1.953 ms |   467.00 ms |
| NUnit     | 4.4.0   |   937.52 ms | 18.192 ms | 19.465 ms |   935.06 ms |
| xUnit     | 2.9.3   | 1,033.10 ms | 18.060 ms | 16.894 ms | 1,028.90 ms |
| MSTest    | 3.11.0  |   885.00 ms | 17.336 ms | 17.026 ms |   882.50 ms |
| xUnit3    | 3.1.0   |   465.87 ms |  3.757 ms |  3.331 ms |   465.39 ms |
| TUnit_AOT | 0.66.6  |    26.79 ms |  0.208 ms |  0.174 ms |    26.82 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit     | 0.66.6  |   527.84 ms | 10.425 ms | 24.160 ms |   521.87 ms |
| NUnit     | 4.4.0   | 1,069.13 ms | 21.368 ms | 22.864 ms | 1,069.02 ms |
| xUnit     | 2.9.3   | 1,152.17 ms | 22.381 ms | 29.102 ms | 1,160.05 ms |
| MSTest    | 3.11.0  |   994.82 ms | 19.730 ms | 21.929 ms |   994.32 ms |
| xUnit3    | 3.1.0   |   544.24 ms | 10.540 ms | 14.071 ms |   543.06 ms |
| TUnit_AOT | 0.66.6  |    68.69 ms |  2.349 ms |  6.815 ms |    68.38 ms |



