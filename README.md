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
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.61.13 | 1.918 s | 0.1560 s | 0.4550 s | 1.812 s |
| Build_NUnit  | 4.4.0   | 1.625 s | 0.1525 s | 0.4497 s | 1.522 s |
| Build_xUnit  | 2.9.3   | 1.268 s | 0.1006 s | 0.2952 s | 1.196 s |
| Build_MSTest | 3.10.4  | 1.532 s | 0.1190 s | 0.3452 s | 1.497 s |



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
| Build_TUnit  | 0.61.13 | 1.836 s | 0.0367 s | 0.0408 s | 1.830 s |
| Build_NUnit  | 4.4.0   | 1.509 s | 0.0134 s | 0.0125 s | 1.511 s |
| Build_xUnit  | 2.9.3   | 1.526 s | 0.0123 s | 0.0115 s | 1.527 s |
| Build_MSTest | 3.10.4  | 1.521 s | 0.0188 s | 0.0176 s | 1.523 s |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.61.13 | 1.973 s | 0.0382 s | 0.0454 s | 1.967 s |
| Build_NUnit  | 4.4.0   | 1.674 s | 0.0181 s | 0.0160 s | 1.674 s |
| Build_xUnit  | 2.9.3   | 1.671 s | 0.0264 s | 0.0247 s | 1.679 s |
| Build_MSTest | 3.10.4  | 1.682 s | 0.0266 s | 0.0236 s | 1.691 s |


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
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   110.9 ms |  2.11 ms |   5.44 ms |   108.9 ms |
| TUnit     | 0.61.13 |   739.7 ms | 54.94 ms | 160.26 ms |   705.9 ms |
| NUnit     | 4.4.0   |         NA |       NA |        NA |         NA |
| xUnit     | 2.9.3   | 1,060.2 ms | 56.19 ms | 165.67 ms | 1,044.3 ms |
| MSTest    | 3.10.4  |   983.8 ms | 60.84 ms | 179.40 ms |   953.0 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)



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
| TUnit_AOT | 0.61.13 |    28.40 ms |  0.531 ms |  0.497 ms |    28.43 ms |
| TUnit     | 0.61.13 |   972.30 ms | 19.263 ms | 20.611 ms |   969.97 ms |
| NUnit     | 4.4.0   |          NA |        NA |        NA |          NA |
| xUnit     | 2.9.3   | 1,413.87 ms | 15.619 ms | 14.610 ms | 1,409.06 ms |
| MSTest    | 3.10.4  | 1,284.40 ms | 16.824 ms | 14.914 ms | 1,285.14 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    64.52 ms |  1.452 ms |  4.189 ms |    63.69 ms |
| TUnit     | 0.61.13 | 1,140.79 ms | 22.787 ms | 44.979 ms | 1,136.18 ms |
| NUnit     | 4.4.0   |          NA |        NA |        NA |          NA |
| xUnit     | 2.9.3   | 1,562.14 ms | 19.368 ms | 17.170 ms | 1,558.80 ms |
| MSTest    | 3.10.4  | 1,453.56 ms | 29.005 ms | 27.131 ms | 1,444.95 ms |

Benchmarks with issues:
  RuntimeBenchmarks.NUnit: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   162.1 ms |  13.28 ms |  38.32 ms |   152.6 ms |
| TUnit     | 0.61.13 |   924.6 ms |  65.33 ms | 189.54 ms |   887.3 ms |
| NUnit     | 4.4.0   | 1,724.4 ms | 151.93 ms | 447.96 ms | 1,706.2 ms |
| xUnit     | 2.9.3   | 1,262.2 ms | 100.37 ms | 291.20 ms | 1,180.1 ms |
| MSTest    | 3.10.4  | 1,003.0 ms |  45.14 ms | 130.24 ms |   972.0 ms |



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
| TUnit_AOT | 0.61.13 |    27.23 ms |  0.512 ms |  0.479 ms |    27.19 ms |
| TUnit     | 0.61.13 |   930.66 ms | 17.773 ms | 21.158 ms |   928.50 ms |
| NUnit     | 4.4.0   | 1,305.07 ms | 11.768 ms | 11.008 ms | 1,305.56 ms |
| xUnit     | 2.9.3   | 1,456.88 ms | 22.017 ms | 20.594 ms | 1,452.03 ms |
| MSTest    | 3.10.4  | 1,283.79 ms | 16.823 ms | 15.736 ms | 1,281.50 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    70.22 ms |  1.403 ms |  3.079 ms |    69.96 ms |
| TUnit     | 0.61.13 | 1,133.62 ms | 25.501 ms | 71.508 ms | 1,119.23 ms |
| NUnit     | 4.4.0   | 1,605.22 ms | 19.582 ms | 34.296 ms | 1,608.29 ms |
| xUnit     | 2.9.3   | 1,711.36 ms | 23.829 ms | 22.289 ms | 1,715.93 ms |
| MSTest    | 3.10.4  | 1,422.68 ms | 27.860 ms | 45.775 ms | 1,403.96 ms |


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
| TUnit_AOT | 0.61.13 |   188.5 ms |  18.39 ms |  54.22 ms |   172.9 ms |
| TUnit     | 0.61.13 | 1,311.5 ms | 115.60 ms | 337.20 ms | 1,347.2 ms |
| NUnit     | 4.4.0   | 1,485.0 ms | 102.18 ms | 299.66 ms | 1,472.0 ms |
| xUnit     | 2.9.3   | 1,203.7 ms | 111.84 ms | 320.89 ms | 1,114.6 ms |
| MSTest    | 3.10.4  |   924.4 ms |  44.11 ms | 125.12 ms |   927.6 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 3.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    26.66 ms |  0.498 ms |  1.051 ms |    26.68 ms |
| TUnit     | 0.61.13 |   913.18 ms | 17.930 ms | 21.344 ms |   912.30 ms |
| NUnit     | 4.4.0   | 1,270.94 ms | 17.391 ms | 16.268 ms | 1,277.04 ms |
| xUnit     | 2.9.3   | 1,333.17 ms | 14.489 ms | 13.553 ms | 1,333.56 ms |
| MSTest    | 3.10.4  | 1,214.79 ms | 14.611 ms | 13.668 ms | 1,215.54 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    63.84 ms |  1.394 ms |  4.067 ms |    63.36 ms |
| TUnit     | 0.61.13 | 1,070.82 ms | 20.806 ms | 23.960 ms | 1,062.79 ms |
| NUnit     | 4.4.0   | 1,451.12 ms | 15.863 ms | 14.838 ms | 1,447.88 ms |
| xUnit     | 2.9.3   | 1,512.73 ms | 13.653 ms | 11.401 ms | 1,513.74 ms |
| MSTest    | 3.10.4  | 1,427.97 ms | 24.734 ms | 21.926 ms | 1,430.61 ms |


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
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   150.6 ms |   9.58 ms |  27.48 ms |   146.4 ms |
| TUnit     | 0.61.13 | 1,061.2 ms | 107.15 ms | 309.16 ms | 1,004.8 ms |
| NUnit     | 4.4.0   | 1,392.6 ms | 128.04 ms | 363.23 ms | 1,334.9 ms |
| xUnit     | 2.9.3   | 1,199.4 ms |  89.43 ms | 263.69 ms | 1,147.9 ms |
| MSTest    | 3.10.4  |   967.6 ms |  59.53 ms | 168.89 ms |   939.1 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    27.47 ms |  0.538 ms |  0.899 ms |    27.35 ms |
| TUnit     | 0.61.13 |   909.47 ms | 17.837 ms | 19.085 ms |   902.41 ms |
| NUnit     | 4.4.0   | 1,272.36 ms | 10.482 ms |  9.805 ms | 1,274.02 ms |
| xUnit     | 2.9.3   | 1,342.49 ms |  5.426 ms |  4.810 ms | 1,344.06 ms |
| MSTest    | 3.10.4  | 1,217.09 ms |  6.172 ms |  5.774 ms | 1,217.55 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev     | Median      |
|---------- |-------- |------------:|----------:|-----------:|------------:|
| TUnit_AOT | 0.61.13 |    69.27 ms |  1.845 ms |   5.439 ms |    69.29 ms |
| TUnit     | 0.61.13 | 1,144.21 ms | 22.870 ms |  59.442 ms | 1,129.41 ms |
| NUnit     | 4.4.0   | 1,596.92 ms | 31.655 ms |  57.883 ms | 1,596.86 ms |
| xUnit     | 2.9.3   | 1,798.12 ms | 57.406 ms | 164.708 ms | 1,806.71 ms |
| MSTest    | 3.10.4  | 1,561.06 ms | 23.643 ms |  22.116 ms | 1,566.69 ms |


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
| Method    | Version | Mean     | Error    | StdDev    | Median   |
|---------- |-------- |---------:|---------:|----------:|---------:|
| TUnit_AOT | 0.61.13 | 157.2 ms |  9.78 ms |  28.67 ms | 151.0 ms |
| TUnit     | 0.61.13 | 716.3 ms | 44.38 ms | 127.32 ms | 695.7 ms |
| NUnit     | 4.4.0   | 925.9 ms | 41.36 ms | 121.94 ms | 892.5 ms |
| xUnit     | 2.9.3   | 889.7 ms | 26.16 ms |  75.46 ms | 883.8 ms |
| MSTest    | 3.10.4  |       NA |       NA |        NA |       NA |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    27.09 ms |  0.507 ms |  0.474 ms |    26.90 ms |
| TUnit     | 0.61.13 |   944.08 ms | 18.351 ms | 19.635 ms |   946.04 ms |
| NUnit     | 4.4.0   | 1,308.89 ms | 10.780 ms | 10.083 ms | 1,308.97 ms |
| xUnit     | 2.9.3   | 1,387.90 ms | 14.870 ms | 13.909 ms | 1,389.48 ms |
| MSTest    | 3.10.4  |          NA |        NA |        NA |          NA |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev     | Median      |
|---------- |-------- |------------:|----------:|-----------:|------------:|
| TUnit_AOT | 0.61.13 |    63.49 ms |  1.622 ms |   4.756 ms |    62.05 ms |
| TUnit     | 0.61.13 | 1,098.28 ms | 20.921 ms |  47.648 ms | 1,095.75 ms |
| NUnit     | 4.4.0   | 1,494.53 ms | 29.750 ms |  72.415 ms | 1,458.86 ms |
| xUnit     | 2.9.3   | 1,669.62 ms | 48.125 ms | 136.522 ms | 1,647.94 ms |
| MSTest    | 3.10.4  |          NA |        NA |         NA |          NA |

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
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   211.4 ms |  16.11 ms |  47.50 ms |   212.1 ms |
| TUnit     | 0.61.13 | 1,074.9 ms | 108.97 ms | 317.86 ms | 1,016.4 ms |
| NUnit     | 4.4.0   | 1,147.3 ms |  72.65 ms | 211.91 ms | 1,123.8 ms |
| xUnit     | 2.9.3   |   946.3 ms |  43.38 ms | 127.89 ms |   930.7 ms |
| MSTest    | 3.10.4  |   829.3 ms |  30.12 ms |  88.33 ms |   821.0 ms |



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
| TUnit_AOT | 0.61.13 |    26.92 ms |  0.465 ms |  0.435 ms |    26.80 ms |
| TUnit     | 0.61.13 |   939.12 ms | 18.640 ms | 24.884 ms |   928.86 ms |
| NUnit     | 4.4.0   | 1,336.50 ms |  7.967 ms |  7.063 ms | 1,338.63 ms |
| xUnit     | 2.9.3   | 1,410.05 ms |  9.572 ms |  8.953 ms | 1,409.42 ms |
| MSTest    | 3.10.4  | 1,288.37 ms | 13.379 ms | 11.860 ms | 1,286.14 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    67.61 ms |  1.937 ms |  5.710 ms |    66.98 ms |
| TUnit     | 0.61.13 | 1,172.75 ms | 23.190 ms | 39.378 ms | 1,167.36 ms |
| NUnit     | 4.4.0   | 1,622.89 ms | 19.758 ms | 18.481 ms | 1,622.22 ms |
| xUnit     | 2.9.3   | 1,735.38 ms | 28.309 ms | 25.095 ms | 1,729.16 ms |
| MSTest    | 3.10.4  | 1,548.46 ms | 28.994 ms | 73.272 ms | 1,554.57 ms |


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
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   315.6 ms | 26.51 ms |  78.17 ms |   314.5 ms |
| TUnit     | 0.61.13 | 1,151.9 ms | 92.54 ms | 271.39 ms | 1,070.6 ms |
| NUnit     | 4.4.0   | 1,002.5 ms | 66.38 ms | 188.32 ms |   970.0 ms |
| xUnit     | 2.9.3   |   986.2 ms | 48.20 ms | 139.85 ms |   984.7 ms |
| MSTest    | 3.10.4  | 1,040.7 ms | 43.33 ms | 127.76 ms | 1,008.0 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    38.91 ms |  0.303 ms |  0.253 ms |    38.87 ms |
| TUnit     | 0.61.13 |   950.57 ms | 18.019 ms | 22.129 ms |   945.27 ms |
| NUnit     | 4.4.0   | 1,301.97 ms | 15.537 ms | 14.533 ms | 1,299.41 ms |
| xUnit     | 2.9.3   | 1,461.88 ms | 15.843 ms | 14.819 ms | 1,463.17 ms |
| MSTest    | 3.10.4  | 1,324.48 ms | 12.798 ms | 11.345 ms | 1,327.26 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    73.73 ms |  1.441 ms |  1.541 ms |    73.50 ms |
| TUnit     | 0.61.13 | 1,093.55 ms | 21.088 ms | 28.866 ms | 1,084.83 ms |
| NUnit     | 4.4.0   | 1,513.95 ms | 30.249 ms | 74.768 ms | 1,476.68 ms |
| xUnit     | 2.9.3   | 1,675.04 ms | 33.426 ms | 45.754 ms | 1,662.99 ms |
| MSTest    | 3.10.4  | 1,488.84 ms | 22.205 ms | 19.684 ms | 1,492.05 ms |


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
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.61.13 |   231.4 ms |  20.05 ms |  59.12 ms |   229.8 ms |
| TUnit     | 0.61.13 | 1,088.3 ms |  82.32 ms | 242.71 ms | 1,046.1 ms |
| NUnit     | 4.4.0   | 1,198.5 ms | 100.16 ms | 293.75 ms | 1,129.5 ms |
| xUnit     | 2.9.3   |   835.6 ms |  16.51 ms |  43.50 ms |   839.8 ms |
| MSTest    | 3.10.4  |   772.8 ms |  17.93 ms |  52.57 ms |   785.9 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    28.02 ms |  0.554 ms |  1.358 ms |    28.10 ms |
| TUnit     | 0.61.13 |   925.55 ms | 17.858 ms | 19.107 ms |   925.64 ms |
| NUnit     | 4.4.0   | 1,288.93 ms |  9.420 ms |  8.351 ms | 1,290.03 ms |
| xUnit     | 2.9.3   | 1,352.49 ms |  8.971 ms |  7.491 ms | 1,354.17 ms |
| MSTest    | 3.10.4  | 1,234.98 ms |  9.729 ms |  9.100 ms | 1,234.56 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.61.13 |    66.57 ms |  1.882 ms |  5.491 ms |    66.53 ms |
| TUnit     | 0.61.13 | 1,105.22 ms | 21.302 ms | 51.446 ms | 1,104.05 ms |
| NUnit     | 4.4.0   | 1,550.45 ms | 30.506 ms | 49.261 ms | 1,560.91 ms |
| xUnit     | 2.9.3   | 1,692.52 ms | 33.832 ms | 33.228 ms | 1,687.74 ms |
| MSTest    | 3.10.4  | 1,543.52 ms | 30.775 ms | 57.802 ms | 1,540.19 ms |



