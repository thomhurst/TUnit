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

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.57.24 | 1.804 s | 0.1526 s | 0.4474 s | 1.713 s |
| Build_NUnit  | 4.4.0   | 1.679 s | 0.1364 s | 0.3978 s | 1.670 s |
| Build_xUnit  | 2.9.3   | 1.405 s | 0.0778 s | 0.2168 s | 1.366 s |
| Build_MSTest | 3.10.4  | 1.419 s | 0.0720 s | 0.2054 s | 1.340 s |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.57.24 | 1.855 s | 0.0368 s | 0.0673 s | 1.843 s |
| Build_NUnit  | 4.4.0   | 1.529 s | 0.0177 s | 0.0157 s | 1.531 s |
| Build_xUnit  | 2.9.3   | 1.540 s | 0.0163 s | 0.0152 s | 1.539 s |
| Build_MSTest | 3.10.4  | 1.528 s | 0.0103 s | 0.0091 s | 1.530 s |



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method       | Version | Mean    | Error    | StdDev   | Median  |
|------------- |-------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 0.57.24 | 1.857 s | 0.0331 s | 0.0543 s | 1.849 s |
| Build_NUnit  | 4.4.0   | 1.580 s | 0.0295 s | 0.0276 s | 1.575 s |
| Build_xUnit  | 2.9.3   | 1.573 s | 0.0238 s | 0.0222 s | 1.584 s |
| Build_MSTest | 3.10.4  | 1.576 s | 0.0165 s | 0.0154 s | 1.578 s |


### Scenario: Tests focused on assertion performance and validation

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.57.24 |   215.8 ms |  15.89 ms |  46.59 ms |   213.8 ms |
| TUnit     | 0.57.24 |   915.1 ms | 102.04 ms | 294.42 ms |   855.5 ms |
| NUnit     | 4.4.0   | 1,096.4 ms |  69.45 ms | 191.29 ms | 1,052.4 ms |
| xUnit     | 2.9.3   | 1,408.4 ms |  84.06 ms | 242.54 ms | 1,402.2 ms |
| MSTest    | 3.10.4  | 1,000.3 ms |  93.83 ms | 270.71 ms |   876.2 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    26.89 ms |  0.197 ms |  0.174 ms |    26.84 ms |
| TUnit     | 0.57.24 |   948.77 ms | 17.984 ms | 17.663 ms |   950.87 ms |
| NUnit     | 4.4.0   | 1,324.22 ms | 13.460 ms | 11.932 ms | 1,325.68 ms |
| xUnit     | 2.9.3   | 1,415.03 ms | 10.329 ms |  8.625 ms | 1,417.82 ms |
| MSTest    | 3.10.4  | 1,265.88 ms |  8.170 ms |  7.642 ms | 1,267.01 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    64.32 ms |  1.247 ms |  2.048 ms |    64.28 ms |
| TUnit     | 0.57.24 | 1,016.76 ms | 20.037 ms | 27.426 ms | 1,018.21 ms |
| NUnit     | 4.4.0   | 1,424.02 ms | 24.174 ms | 22.612 ms | 1,424.85 ms |
| xUnit     | 2.9.3   | 1,493.27 ms | 22.530 ms | 21.074 ms | 1,493.30 ms |
| MSTest    | 3.10.4  | 1,325.92 ms | 18.727 ms | 17.517 ms | 1,325.13 ms |


### Scenario: Simple tests with basic operations and assertions

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.57.24 |   191.4 ms |  20.17 ms |  58.21 ms |   163.3 ms |
| TUnit     | 0.57.24 | 1,409.8 ms | 104.33 ms | 305.99 ms | 1,431.3 ms |
| NUnit     | 4.4.0   | 1,495.4 ms |  99.31 ms | 283.33 ms | 1,451.8 ms |
| xUnit     | 2.9.3   | 1,206.7 ms |  82.32 ms | 234.87 ms | 1,174.9 ms |
| MSTest    | 3.10.4  | 1,232.4 ms |  54.91 ms | 159.31 ms | 1,232.5 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    26.92 ms |  0.173 ms |  0.145 ms |    26.90 ms |
| TUnit     | 0.57.24 |   975.45 ms | 19.078 ms | 21.205 ms |   974.07 ms |
| NUnit     | 4.4.0   | 1,362.29 ms | 11.943 ms | 10.587 ms | 1,362.19 ms |
| xUnit     | 2.9.3   | 1,412.37 ms | 20.066 ms | 17.788 ms | 1,413.50 ms |
| MSTest    | 3.10.4  | 1,308.54 ms | 15.830 ms | 14.808 ms | 1,313.33 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    61.61 ms |  1.201 ms |  1.562 ms |    62.38 ms |
| TUnit     | 0.57.24 | 1,017.21 ms | 20.293 ms | 24.157 ms | 1,016.80 ms |
| NUnit     | 4.4.0   | 1,360.28 ms | 12.501 ms | 11.082 ms | 1,357.52 ms |
| xUnit     | 2.9.3   | 1,403.83 ms | 14.496 ms | 13.560 ms | 1,403.25 ms |
| MSTest    | 3.10.4  | 1,286.28 ms |  9.742 ms |  9.112 ms | 1,288.01 ms |


### Scenario: Parameterized tests with multiple test cases using data attributes

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.57.24 |      NA |       NA |       NA |      NA |
| TUnit     | 0.57.24 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.739 s | 0.1286 s | 0.3670 s | 1.613 s |
| xUnit     | 2.9.3   | 1.250 s | 0.0569 s | 0.1651 s | 1.239 s |
| MSTest    | 3.10.4  | 1.219 s | 0.0690 s | 0.1991 s | 1.168 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.57.24 |      NA |       NA |       NA |      NA |
| TUnit     | 0.57.24 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.287 s | 0.0172 s | 0.0161 s | 1.285 s |
| xUnit     | 2.9.3   | 1.354 s | 0.0104 s | 0.0098 s | 1.354 s |
| MSTest    | 3.10.4  | 1.229 s | 0.0065 s | 0.0061 s | 1.228 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit_AOT | 0.57.24 |      NA |       NA |       NA |      NA |
| TUnit     | 0.57.24 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.326 s | 0.0101 s | 0.0089 s | 1.325 s |
| xUnit     | 2.9.3   | 1.390 s | 0.0153 s | 0.0143 s | 1.392 s |
| MSTest    | 3.10.4  | 1.276 s | 0.0143 s | 0.0127 s | 1.278 s |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests utilizing class fixtures and shared test context

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median     |
|---------- |-------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT | 0.57.24 |   154.8 ms |  11.10 ms |  32.21 ms |   150.9 ms |
| TUnit     | 0.57.24 | 1,090.7 ms | 103.10 ms | 302.37 ms | 1,025.5 ms |
| NUnit     | 4.4.0   | 1,177.5 ms |  78.14 ms | 221.67 ms | 1,143.8 ms |
| xUnit     | 2.9.3   | 1,030.9 ms |  49.14 ms | 136.16 ms |   996.0 ms |
| MSTest    | 3.10.4  |   855.6 ms |  26.83 ms |  77.85 ms |   851.9 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    27.31 ms |  0.504 ms |  0.539 ms |    27.15 ms |
| TUnit     | 0.57.24 |   959.89 ms | 18.374 ms | 19.660 ms |   952.92 ms |
| NUnit     | 4.4.0   | 1,330.82 ms | 12.228 ms | 11.438 ms | 1,326.79 ms |
| xUnit     | 2.9.3   | 1,415.30 ms | 19.792 ms | 18.513 ms | 1,406.65 ms |
| MSTest    | 3.10.4  |          NA |        NA |        NA |          NA |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    51.55 ms |  1.003 ms |  1.114 ms |    51.77 ms |
| TUnit     | 0.57.24 | 1,004.77 ms | 19.240 ms | 21.385 ms | 1,009.81 ms |
| NUnit     | 4.4.0   | 1,339.82 ms |  8.317 ms |  7.373 ms | 1,341.87 ms |
| xUnit     | 2.9.3   | 1,402.09 ms | 16.295 ms | 14.445 ms | 1,398.96 ms |
| MSTest    | 3.10.4  |          NA |        NA |        NA |          NA |

Benchmarks with issues:
  RuntimeBenchmarks.MSTest: Job-YNJDZW(Runtime=.NET 9.0)


### Scenario: Tests executing in parallel to test framework parallelization

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean       | Error     | StdDev    | Median   |
|---------- |-------- |-----------:|----------:|----------:|---------:|
| TUnit_AOT | 0.57.24 |   133.8 ms |   6.64 ms |  19.48 ms | 126.4 ms |
| TUnit     | 0.57.24 | 1,009.1 ms | 105.06 ms | 309.77 ms | 885.6 ms |
| NUnit     | 4.4.0   | 1,006.7 ms |  50.58 ms | 148.35 ms | 974.1 ms |
| xUnit     | 2.9.3   |   864.4 ms |  27.97 ms |  82.48 ms | 856.1 ms |
| MSTest    | 3.10.4  |   806.0 ms |  23.59 ms |  68.81 ms | 809.1 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    25.96 ms |  0.238 ms |  0.211 ms |    25.93 ms |
| TUnit     | 0.57.24 |   932.70 ms | 17.949 ms | 20.670 ms |   928.36 ms |
| NUnit     | 4.4.0   | 1,303.32 ms | 10.079 ms |  9.428 ms | 1,301.83 ms |
| xUnit     | 2.9.3   | 1,372.69 ms |  4.940 ms |  4.125 ms | 1,374.68 ms |
| MSTest    | 3.10.4  | 1,248.73 ms |  7.087 ms |  6.629 ms | 1,248.81 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    47.44 ms |  0.926 ms |  1.137 ms |    46.80 ms |
| TUnit     | 0.57.24 |   976.43 ms | 19.139 ms | 26.197 ms |   964.54 ms |
| NUnit     | 4.4.0   | 1,314.75 ms |  6.714 ms |  5.952 ms | 1,316.70 ms |
| xUnit     | 2.9.3   | 1,374.20 ms | 15.436 ms | 14.439 ms | 1,371.95 ms |
| MSTest    | 3.10.4  | 1,258.69 ms |  9.691 ms |  9.065 ms | 1,257.25 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times

#### macos-latest

```

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean | Error | StdDev | Median |
|---------- |-------- |-----:|------:|-------:|-------:|
| TUnit_AOT | 0.57.24 |   NA |    NA |     NA |     NA |
| TUnit     | 0.57.24 |   NA |    NA |     NA |     NA |
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

BenchmarkDotNet v0.15.2, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD

Runtime=.NET 9.0  

```
| Method    | Version | Mean     | Error    | StdDev    | Median   |
|---------- |-------- |---------:|---------:|----------:|---------:|
| TUnit_AOT | 0.57.24 | 136.7 ms | 10.01 ms |  28.89 ms | 132.8 ms |
| TUnit     | 0.57.24 | 576.4 ms | 13.13 ms |  37.87 ms | 562.6 ms |
| NUnit     | 4.4.0   | 970.2 ms | 66.25 ms | 192.20 ms | 939.0 ms |
| xUnit     | 2.9.3   | 886.2 ms | 26.36 ms |  73.90 ms | 859.2 ms |
| MSTest    | 3.10.4  | 744.8 ms | 17.75 ms |  50.93 ms | 718.8 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    25.72 ms |  0.158 ms |  0.140 ms |    25.66 ms |
| TUnit     | 0.57.24 |   939.64 ms | 18.504 ms | 24.061 ms |   940.77 ms |
| NUnit     | 4.4.0   | 1,286.37 ms |  6.530 ms |  5.789 ms | 1,284.69 ms |
| xUnit     | 2.9.3   | 1,358.85 ms |  8.468 ms |  7.507 ms | 1,359.20 ms |
| MSTest    | 3.10.4  | 1,235.76 ms |  8.849 ms |  8.278 ms | 1,234.92 ms |



#### windows-latest

```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.20348.4052) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-YNJDZW : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

Runtime=.NET 9.0  

```
| Method    | Version | Mean        | Error     | StdDev    | Median      |
|---------- |-------- |------------:|----------:|----------:|------------:|
| TUnit_AOT | 0.57.24 |    56.03 ms |  1.090 ms |  1.298 ms |    55.51 ms |
| TUnit     | 0.57.24 | 1,040.38 ms | 20.530 ms | 22.819 ms | 1,036.25 ms |
| NUnit     | 4.4.0   | 1,377.60 ms | 14.445 ms | 13.512 ms | 1,377.17 ms |
| xUnit     | 2.9.3   | 1,455.63 ms | 27.581 ms | 27.088 ms | 1,446.46 ms |
| MSTest    | 3.10.4  | 1,341.71 ms | 21.227 ms | 19.856 ms | 1,338.69 ms |



