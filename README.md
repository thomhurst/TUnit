![](assets/banner.png)

# 🚀 The Modern Testing Framework for .NET

**TUnit** is a next-generation testing framework for C# that outpaces traditional frameworks with **source-generated tests**, **parallel execution by default**, and **Native AOT support**. Built on the modern Microsoft.Testing.Platform, TUnit delivers faster test runs, better developer experience, and unmatched flexibility.

<div align="center">

[![thomhurst%2FTUnit | Trendshift](https://trendshift.io/api/badge/repositories/11781)](https://trendshift.io/repositories/11781)


[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a8231644d844435eb9fd15110ea771d8)](https://app.codacy.com/gh/thomhurst/TUnit?utm_source=github.com&utm_medium=referral&utm_content=thomhurst/TUnit&utm_campaign=Badge_Grade)![GitHub Repo stars](https://img.shields.io/github/stars/thomhurst/TUnit) ![GitHub Issues or Pull Requests](https://img.shields.io/github/issues-closed-raw/thomhurst/TUnit)
 [![GitHub Sponsors](https://img.shields.io/github/sponsors/thomhurst)](https://github.com/sponsors/thomhurst) [![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) [![NuGet Downloads](https://img.shields.io/nuget/dt/TUnit)](https://www.nuget.org/packages/TUnit/) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit)

</div>

## ⚡ Why Choose TUnit?

| Feature | Traditional Frameworks | **TUnit** |
|---------|----------------------|-----------|
| Test Discovery | ❌ Runtime reflection | ✅ **Compile-time generation** |
| Execution Speed | ❌ Sequential by default | ✅ **Parallel by default** |
| Modern .NET | ⚠️ Limited AOT support | ✅ **Full Native AOT & trimming** |
| Test Dependencies | ❌ Not supported | ✅ **`[DependsOn]` chains** |
| Resource Management | ❌ Manual lifecycle | ✅ **Intelligent cleanup** |

⚡ **Parallel by Default** - Tests run concurrently with intelligent dependency management

🎯 **Compile-Time Discovery** - Know your test structure before runtime

🔧 **Modern .NET Ready** - Native AOT, trimming, and latest .NET features

🎭 **Extensible** - Customize data sources, attributes, and test behavior

---

<div align="center">

## 📚 **[Complete Documentation & Learning Center](https://tunit.dev)**

**🚀 New to TUnit?** Start with our **[Getting Started Guide](https://tunit.dev/docs/getting-started/installation)**

**🔄 Migrating?** See our **[Migration Guides](https://tunit.dev/docs/migration/xunit)**

**🎯 Advanced Features?** Explore **[Data-Driven Testing](https://tunit.dev/docs/test-authoring/arguments)**, **[Test Dependencies](https://tunit.dev/docs/test-authoring/depends-on)**, and **[Parallelism Control](https://tunit.dev/docs/parallelism/not-in-parallel)**

</div>

---

## 🏁 Quick Start

### Using the Project Template (Recommended)
```bash
dotnet new install TUnit.Templates
dotnet new TUnit -n "MyTestProject"
```

### Manual Installation
```bash
dotnet add package TUnit --prerelease
```

📖 **[📚 Complete Documentation & Guides](https://tunit.dev)** - Everything you need to master TUnit

## ✨ Key Features

<table>
<tr>
<td width="50%">

**🚀 Performance & Modern Platform**
- 🔥 Source-generated tests (no reflection)
- ⚡ Parallel execution by default
- 🚀 Native AOT & trimming support
- 📈 Optimized for performance

</td>
<td width="50%">

**🎯 Advanced Test Control**
- 🔗 Test dependencies with `[DependsOn]`
- 🎛️ Parallel limits & custom scheduling
- 🛡️ Built-in analyzers & compile-time checks
- 🎭 Custom attributes & extensible conditions

</td>
</tr>
<tr>
<td>

**📊 Rich Data & Assertions**
- 📋 Multiple data sources (`[Arguments]`, `[Matrix]`, `[ClassData]`)
- ✅ Fluent async assertions
- 🔄 Smart retry logic & conditional execution
- 📝 Rich test metadata & context

</td>
<td>

**🔧 Developer Experience**
- 💉 Full dependency injection support
- 🪝 Comprehensive lifecycle hooks
- 🎯 IDE integration (VS, Rider, VS Code)
- 📚 Extensive documentation & examples

</td>
</tr>
</table>

## 📝 Simple Test Example

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

## 🎯 Data-Driven Testing

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

## 🔗 Advanced Test Orchestration

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

## 🔧 Smart Test Control

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

## 🎯 Perfect For Every Testing Scenario

<table>
<tr>
<td width="33%">

### 🧪 **Unit Testing**
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
**Fast, isolated, and reliable**

</td>
<td width="33%">

### 🔗 **Integration Testing**
```csharp
[Test, DependsOn(nameof(CreateUser))]
public async Task Login_After_Registration()
{
    // Runs after CreateUser completes
    var result = await authService.Login(user);
    await Assert.That(result.IsSuccess).IsTrue();
}
```
**Stateful workflows made simple**

</td>
<td width="33%">

### ⚡ **Load Testing**
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
**Built-in performance testing**

</td>
</tr>
</table>

## 🚀 What Makes TUnit Different?

### **Compile-Time Intelligence**
Tests are discovered at build time, not runtime - enabling faster discovery, better IDE integration, and precise resource lifecycle management.

### **Parallel-First Architecture**
Built for concurrency from day one with `[DependsOn]` for test chains, `[ParallelLimit]` for resource control, and intelligent scheduling.

### **Extensible by Design**
The `DataSourceGenerator<T>` pattern and custom attribute system let you extend TUnit's capabilities without modifying core framework code.

## 🏆 Community & Ecosystem

<div align="center">

**🌟 Join thousands of developers modernizing their testing**

[![Downloads](https://img.shields.io/nuget/dt/TUnit?label=Downloads&color=blue)](https://www.nuget.org/packages/TUnit/)
[![Contributors](https://img.shields.io/github/contributors/thomhurst/TUnit?label=Contributors)](https://github.com/thomhurst/TUnit/graphs/contributors)
[![Discussions](https://img.shields.io/github/discussions/thomhurst/TUnit?label=Discussions)](https://github.com/thomhurst/TUnit/discussions)

</div>

### 🤝 **Active Community**
- 📚 **[Official Documentation](https://tunit.dev)** - Comprehensive guides, tutorials, and API reference
- 💬 **[GitHub Discussions](https://github.com/thomhurst/TUnit/discussions)** - Get help and share ideas
- 🐛 **[Issue Tracking](https://github.com/thomhurst/TUnit/issues)** - Report bugs and request features
- 📢 **[Release Notes](https://github.com/thomhurst/TUnit/releases)** - Stay updated with latest improvements

## 🛠️ IDE Support

TUnit works seamlessly across all major .NET development environments:

### Visual Studio (2022 17.13+)
✅ **Fully supported** - No additional configuration needed for latest versions

⚙️ **Earlier versions**: Enable "Use testing platform server mode" in Tools > Manage Preview Features

### JetBrains Rider
✅ **Fully supported**

⚙️ **Setup**: Enable "Testing Platform support" in Settings > Build, Execution, Deployment > Unit Testing > VSTest

### Visual Studio Code
✅ **Fully supported**

⚙️ **Setup**: Install [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) and enable "Use Testing Platform Protocol"

### Command Line
✅ **Full CLI support** - Works with `dotnet test`, `dotnet run`, and direct executable execution

## 📦 Package Options

| Package | Use Case |
|---------|----------|
| **`TUnit`** | ⭐ **Start here** - Complete testing framework (includes Core + Engine + Assertions) |
| **`TUnit.Core`** | 📚 Test libraries and shared components (no execution engine) |
| **`TUnit.Engine`** | 🚀 Test execution engine and adapter (for test projects) |
| **`TUnit.Assertions`** | ✅ Standalone assertions (works with any test framework) |
| **`TUnit.Playwright`** | 🎭 Playwright integration with automatic lifecycle management |

## 🎯 Migration from Other Frameworks

**Coming from NUnit or xUnit?** TUnit maintains familiar syntax while adding modern capabilities:

```csharp
// Enhanced with TUnit's advanced features
[Test]
[Arguments("value1")]
[Arguments("value2")]
[Retry(3)]
[ParallelLimit<CustomLimit>]
public async Task Modern_TUnit_Test(string value) { }
```

📖 **Need help migrating?** Check our detailed **[Migration Guides](https://tunit.dev/docs/migration/xunit)** with step-by-step instructions for xUnit, NUnit, and MSTest.


## 💡 Current Status

The API is mostly stable, but may have some changes based on feedback or issues before v1.0 release.

---

<div align="center">

## 🚀 Ready to Experience the Future of .NET Testing?

### ⚡ **Start in 30 Seconds**

```bash
# Create a new test project with examples
dotnet new install TUnit.Templates && dotnet new TUnit -n "MyAwesomeTests"

# Or add to existing project
dotnet add package TUnit --prerelease
```

### 🎯 **Why Wait? Join the Movement**

<table>
<tr>
<td align="center" width="25%">

### 📈 **Performance**
**Optimized execution**
**Parallel by default**
**Zero reflection overhead**

</td>
<td align="center" width="25%">

### 🔮 **Future-Ready**
**Native AOT support**
**Latest .NET features**
**Source generation**

</td>
<td align="center" width="25%">

### 🛠️ **Developer Experience**
**Compile-time checks**
**Rich IDE integration**
**Intelligent debugging**

</td>
<td align="center" width="25%">

### 🎭 **Flexibility**
**Test dependencies**
**Custom attributes**
**Extensible architecture**

</td>
</tr>
</table>

---

**📖 Learn More**: [tunit.dev](https://tunit.dev) | **💬 Get Help**: [GitHub Discussions](https://github.com/thomhurst/TUnit/discussions) | **⭐ Show Support**: [Star on GitHub](https://github.com/thomhurst/TUnit)

*TUnit is actively developed and production-ready. Join our growing community of developers who've made the switch!*

</div>

## Performance Benchmark

### Scenario: Building the test project

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method       | Version | Mean       | Error    | StdDev    | Median     |
|------------- |-------- |-----------:|---------:|----------:|-----------:|
| Build_TUnit  | 0.61.2  | 1,170.4 ms | 42.14 ms | 121.59 ms | 1,159.2 ms |
| Build_NUnit  | 4.4.0   |   982.9 ms | 34.13 ms | 100.10 ms |   941.9 ms |
| Build_xUnit  | 2.9.3   |   939.6 ms | 34.81 ms | 102.63 ms |   887.0 ms |
| Build_MSTest | 3.10.4  | 1,004.5 ms | 32.55 ms |  95.47 ms |   994.7 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.61.2  | 1.814 s | 0.0344 s | 0.0321 s | 1.815 s |
| Build_NUnit  | 4.4.0   | 1.511 s | 0.0139 s | 0.0130 s | 1.512 s |
| Build_xUnit  | 2.9.3   | 1.527 s | 0.0161 s | 0.0142 s | 1.529 s |
| Build_MSTest | 3.10.4  | 1.516 s | 0.0114 s | 0.0107 s | 1.519 s |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.61.2  | 1.915 s | 0.0311 s | 0.0276 s | 1.918 s |
| Build_NUnit  | 4.4.0   | 1.595 s | 0.0270 s | 0.0252 s | 1.599 s |
| Build_xUnit  | 2.9.3   | 1.585 s | 0.0172 s | 0.0152 s | 1.590 s |
| Build_MSTest | 3.10.4  | 1.611 s | 0.0272 s | 0.0254 s | 1.609 s |


### Scenario: Tests focused on assertion performance and validation

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests running asynchronous operations and async/await patterns

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit_AOT | 0.61.2  |   150.6 ms |  3.50 ms |   9.52 ms |   147.6 ms |
| TUnit     | 0.61.2  |   881.8 ms | 32.75 ms |  96.06 ms |   869.0 ms |
| NUnit     | 4.4.0   | 1,332.2 ms | 55.34 ms | 161.44 ms | 1,330.6 ms |
| xUnit     | 2.9.3   | 1,305.7 ms | 46.16 ms | 136.10 ms | 1,307.4 ms |
| MSTest    | 3.10.4  | 1,134.5 ms | 44.33 ms | 128.60 ms | 1,113.4 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    26.70 ms |  0.534 ms |  0.548 ms |    26.41 ms |
| TUnit     | 0.61.2  |   926.51 ms | 18.412 ms | 20.465 ms |   923.46 ms |
| NUnit     | 4.4.0   | 1,294.72 ms |  8.803 ms |  8.235 ms | 1,294.18 ms |
| xUnit     | 2.9.3   | 1,397.16 ms |  8.101 ms |  7.578 ms | 1,397.93 ms |
| MSTest    | 3.10.4  | 1,248.78 ms |  9.050 ms |  8.466 ms | 1,250.14 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    62.38 ms |  0.193 ms |  0.171 ms |    62.41 ms |
| TUnit     | 0.61.2  |   986.67 ms | 19.163 ms | 24.917 ms |   972.03 ms |
| NUnit     | 4.4.0   | 1,364.34 ms | 16.889 ms | 15.798 ms | 1,368.18 ms |
| xUnit     | 2.9.3   | 1,450.47 ms | 10.318 ms |  9.147 ms | 1,452.91 ms |
| MSTest    | 3.10.4  | 1,322.40 ms | 26.176 ms | 28.008 ms | 1,311.00 ms |


### Scenario: Simple tests with basic operations and assertions

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.2  |   242.9 ms |  27.82 ms |  82.03 ms |   239.3 ms |
| TUnit     | 0.61.2  | 1,154.0 ms |  76.26 ms | 224.86 ms | 1,152.9 ms |
| NUnit     | 4.4.0   | 1,682.0 ms | 117.63 ms | 346.85 ms | 1,761.3 ms |
| xUnit     | 2.9.3   | 1,233.7 ms |  47.58 ms | 138.79 ms | 1,218.6 ms |
| MSTest    | 3.10.4  | 1,164.4 ms |  70.58 ms | 205.89 ms | 1,127.9 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    27.90 ms |  0.557 ms |  0.494 ms |    27.72 ms |
| TUnit     | 0.61.2  |   974.56 ms | 19.207 ms | 22.864 ms |   974.14 ms |
| NUnit     | 4.4.0   | 1,376.49 ms | 14.116 ms | 12.514 ms | 1,376.14 ms |
| xUnit     | 2.9.3   | 1,452.99 ms | 13.206 ms | 11.707 ms | 1,451.57 ms |
| MSTest    | 3.10.4  | 1,344.57 ms | 14.546 ms | 13.606 ms | 1,347.15 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    58.87 ms |  1.172 ms |  2.285 ms |    57.71 ms |
| TUnit     | 0.61.2  |   992.84 ms | 19.609 ms | 26.842 ms |   977.26 ms |
| NUnit     | 4.4.0   | 1,345.01 ms |  8.605 ms |  8.049 ms | 1,345.15 ms |
| xUnit     | 2.9.3   | 1,398.69 ms |  8.313 ms |  7.369 ms | 1,399.00 ms |
| MSTest    | 3.10.4  | 1,297.70 ms | 14.302 ms | 13.378 ms | 1,297.22 ms |


### Scenario: Parameterized tests with multiple test cases using data attributes

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.61.2  |      NA |       NA |       NA |      NA |
| TUnit     | 0.61.2  |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.269 s | 0.0804 s | 0.2347 s | 1.239 s |
| xUnit     | 2.9.3   | 1.280 s | 0.0736 s | 0.2169 s | 1.232 s |
| MSTest    | 3.10.4  | 1.314 s | 0.0910 s | 0.2682 s | 1.307 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.61.2  |      NA |       NA |       NA |      NA |
| TUnit     | 0.61.2  |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.377 s | 0.0156 s | 0.0146 s | 1.375 s |
| xUnit     | 2.9.3   | 1.448 s | 0.0107 s | 0.0100 s | 1.445 s |
| MSTest    | 3.10.4  | 1.322 s | 0.0134 s | 0.0118 s | 1.321 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.61.2  |      NA |       NA |       NA |      NA |
| TUnit     | 0.61.2  |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.415 s | 0.0252 s | 0.0235 s | 1.412 s |
| xUnit     | 2.9.3   | 1.398 s | 0.0196 s | 0.0183 s | 1.407 s |
| MSTest    | 3.10.4  | 1.278 s | 0.0249 s | 0.0266 s | 1.270 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests utilizing class fixtures and shared test context

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.2  |   256.1 ms |  19.79 ms |  58.03 ms |   250.1 ms |
| TUnit     | 0.61.2  | 1,043.5 ms |  78.62 ms | 230.59 ms | 1,052.8 ms |
| NUnit     | 4.4.0   | 1,121.9 ms | 104.40 ms | 307.84 ms |   969.7 ms |
| xUnit     | 2.9.3   |   858.7 ms |  25.19 ms |  74.28 ms |   854.1 ms |
| MSTest    | 3.10.4  |   793.4 ms |  15.79 ms |  42.69 ms |   794.1 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    27.11 ms |  0.232 ms |  0.194 ms |    27.09 ms |
| TUnit     | 0.61.2  |   938.34 ms | 18.432 ms | 20.487 ms |   939.40 ms |
| NUnit     | 4.4.0   | 1,301.77 ms |  8.705 ms |  7.717 ms | 1,303.23 ms |
| xUnit     | 2.9.3   | 1,377.75 ms | 10.216 ms |  8.531 ms | 1,379.43 ms |
| MSTest    | 3.10.4  | 1,246.52 ms |  9.031 ms |  8.448 ms | 1,246.94 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    55.52 ms |  1.879 ms |  5.541 ms |    54.78 ms |
| TUnit     | 0.61.2  |   973.20 ms | 19.171 ms | 26.874 ms |   958.26 ms |
| NUnit     | 4.4.0   | 1,315.93 ms | 12.030 ms | 11.253 ms | 1,316.45 ms |
| xUnit     | 2.9.3   | 1,383.54 ms | 16.294 ms | 15.241 ms | 1,385.02 ms |
| MSTest    | 3.10.4  |          NA |        NA |        NA |          NA |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests executing in parallel to test framework parallelization

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean     | Error    | StdDev   | Median   |
|---------- |-------- |---------:|---------:|---------:|---------:|
| TUnit_AOT | 0.61.2  | 114.4 ms |  4.81 ms | 13.33 ms | 109.5 ms |
| TUnit     | 0.61.2  | 608.4 ms | 18.25 ms | 53.22 ms | 607.9 ms |
| NUnit     | 4.4.0   | 822.8 ms | 19.43 ms | 55.74 ms | 818.5 ms |
| xUnit     | 2.9.3   | 839.0 ms | 16.78 ms | 41.47 ms | 842.4 ms |
| MSTest    | 3.10.4  | 773.4 ms | 15.45 ms | 41.25 ms | 776.8 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    27.55 ms |  0.536 ms |  0.551 ms |    27.33 ms |
| TUnit     | 0.61.2  |   942.44 ms | 17.939 ms | 18.422 ms |   934.26 ms |
| NUnit     | 4.4.0   | 1,355.13 ms | 10.891 ms | 10.187 ms | 1,358.40 ms |
| xUnit     | 2.9.3   | 1,427.53 ms | 11.102 ms | 10.385 ms | 1,428.19 ms |
| MSTest    | 3.10.4  | 1,300.29 ms |  9.285 ms |  7.754 ms | 1,302.97 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    58.14 ms |  1.154 ms |  3.402 ms |    58.91 ms |
| TUnit     | 0.61.2  |   975.81 ms | 19.340 ms | 25.818 ms |   958.05 ms |
| NUnit     | 4.4.0   | 1,350.65 ms | 13.990 ms | 13.087 ms | 1,351.49 ms |
| xUnit     | 2.9.3   | 1,414.29 ms | 15.152 ms | 14.173 ms | 1,408.87 ms |
| MSTest    | 3.10.4  | 1,302.79 ms | 12.261 ms | 10.869 ms | 1,301.63 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.61.2  |   NA |    NA |     NA |     NA |
| TUnit     | 0.61.2  |   NA |    NA |     NA |     NA |
| NUnit     | 4.4.0   |   NA |    NA |     NA |     NA |
| xUnit     | 2.9.3   |   NA |    NA |     NA |     NA |
| MSTest    | 3.10.4  |   NA |    NA |     NA |     NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.xUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests with setup and teardown lifecycle methods

#### macos-latest

```

BenchmarkDotNet v0.15.3, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method    | Version | Mean     | Error    | StdDev   | Median   |
|---------- |-------- |---------:|---------:|---------:|---------:|
| TUnit_AOT | 0.61.2  | 114.3 ms |  1.10 ms |  1.43 ms | 114.1 ms |
| TUnit     | 0.61.2  | 613.6 ms | 12.22 ms | 34.27 ms | 612.2 ms |
| NUnit     | 4.4.0   | 877.4 ms | 28.01 ms | 80.38 ms | 864.9 ms |
| xUnit     | 2.9.3   | 873.4 ms | 29.26 ms | 86.29 ms | 850.5 ms |
| MSTest    | 3.10.4  | 744.5 ms |  9.21 ms |  7.19 ms | 747.3 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    28.95 ms |  0.575 ms |  1.261 ms |    28.92 ms |
| TUnit     | 0.61.2  |   934.92 ms | 18.180 ms | 24.270 ms |   928.99 ms |
| NUnit     | 4.4.0   | 1,305.31 ms |  9.723 ms |  8.119 ms | 1,308.16 ms |
| xUnit     | 2.9.3   | 1,368.10 ms |  5.280 ms |  4.681 ms | 1,369.14 ms |
| MSTest    | 3.10.4  | 1,258.61 ms | 12.081 ms | 11.300 ms | 1,260.93 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.2  |    64.65 ms |  1.046 ms |  0.817 ms |    64.58 ms |
| TUnit     | 0.61.2  |   995.60 ms | 19.393 ms | 23.086 ms |   987.64 ms |
| NUnit     | 4.4.0   | 1,354.28 ms | 16.238 ms | 15.189 ms | 1,350.60 ms |
| xUnit     | 2.9.3   | 1,416.67 ms | 10.143 ms |  8.992 ms | 1,416.28 ms |
| MSTest    | 3.10.4  | 1,349.30 ms | 20.168 ms | 18.865 ms | 1,345.64 ms |



