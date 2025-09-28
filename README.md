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

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Runtime=.NET 9.0  

```
| Method       | Version | Mean       | Error    | StdDev    | Median     |
|------------- |-------- |-----------:|---------:|----------:|-----------:|
| Build_TUnit  | 0.61.39 | 1,067.9 ms | 35.82 ms | 104.49 ms | 1,056.1 ms |
| Build_NUnit  | 4.4.0   |   977.0 ms | 26.48 ms |  77.65 ms |   958.4 ms |
| Build_xUnit  | 2.9.3   |   929.1 ms | 22.08 ms |  64.41 ms |   908.1 ms |
| Build_MSTest | 3.10.4  | 1,004.6 ms | 28.16 ms |  82.58 ms |   983.6 ms |



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
| Build_TUnit  | 0.61.39 | 1.678 s | 0.0211 s | 0.0176 s | 1.677 s |
| Build_NUnit  | 4.4.0   | 1.502 s | 0.0176 s | 0.0164 s | 1.507 s |
| Build_xUnit  | 2.9.3   | 1.525 s | 0.0211 s | 0.0187 s | 1.527 s |
| Build_MSTest | 3.10.4  | 1.542 s | 0.0187 s | 0.0175 s | 1.545 s |



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
| Build_TUnit  | 0.61.39 | 1.958 s | 0.0390 s | 0.0813 s | 1.931 s |
| Build_NUnit  | 4.4.0   | 1.839 s | 0.0355 s | 0.0885 s | 1.813 s |
| Build_xUnit  | 2.9.3   | 1.834 s | 0.0299 s | 0.0265 s | 1.839 s |
| Build_MSTest | 3.10.4  | 1.865 s | 0.0366 s | 0.0422 s | 1.860 s |


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
| Method    | Version | Mean       | Error    | StdDev   | Median     |
|---------- |-------- |-----------:|---------:|---------:|-----------:|
| TUnit     | 0.61.39 |         NA |       NA |       NA |         NA |
| NUnit     | 4.4.0   | 1,532.0 ms | 76.89 ms | 224.3 ms | 1,536.9 ms |
| xUnit     | 2.9.3   | 1,409.9 ms | 89.99 ms | 265.3 ms | 1,346.9 ms |
| MSTest    | 3.10.4  |   970.4 ms | 79.72 ms | 227.4 ms |   888.0 ms |
| TUnit_AOT | 0.61.39 |         NA |       NA |       NA |         NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.271 s | 0.0246 s | 0.0345 s | 1.254 s |
| xUnit     | 2.9.3   | 1.277 s | 0.0203 s | 0.0170 s | 1.277 s |
| MSTest    | 3.10.4  | 1.271 s | 0.0161 s | 0.0143 s | 1.266 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.402 s | 0.0110 s | 0.0097 s | 1.404 s |
| xUnit     | 2.9.3   | 1.409 s | 0.0104 s | 0.0098 s | 1.406 s |
| MSTest    | 3.10.4  | 1.428 s | 0.0112 s | 0.0105 s | 1.427 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean       | Error     | StdDev   | Median     |
|---------- |-------- |-----------:|----------:|---------:|-----------:|
| TUnit     | 0.61.39 |         NA |        NA |       NA |         NA |
| NUnit     | 4.4.0   | 1,459.2 ms | 142.24 ms | 419.4 ms | 1,340.3 ms |
| xUnit     | 2.9.3   | 1,478.0 ms | 105.42 ms | 310.8 ms | 1,470.2 ms |
| MSTest    | 3.10.4  |   924.4 ms |  44.90 ms | 129.5 ms |   890.1 ms |
| TUnit_AOT | 0.61.39 |         NA |        NA |       NA |         NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.235 s | 0.0143 s | 0.0134 s | 1.233 s |
| xUnit     | 2.9.3   | 1.232 s | 0.0069 s | 0.0064 s | 1.232 s |
| MSTest    | 3.10.4  | 1.229 s | 0.0050 s | 0.0047 s | 1.229 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.540 s | 0.0267 s | 0.0250 s | 1.545 s |
| xUnit     | 2.9.3   | 1.536 s | 0.0160 s | 0.0149 s | 1.541 s |
| MSTest    | 3.10.4  | 1.542 s | 0.0168 s | 0.0157 s | 1.539 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.510 s | 0.1545 s | 0.4555 s | 1.365 s |
| xUnit     | 2.9.3   | 1.727 s | 0.0954 s | 0.2814 s | 1.772 s |
| MSTest    | 3.10.4  | 1.705 s | 0.0970 s | 0.2844 s | 1.733 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.356 s | 0.0114 s | 0.0101 s | 1.357 s |
| xUnit     | 2.9.3   | 1.353 s | 0.0218 s | 0.0204 s | 1.357 s |
| MSTest    | 3.10.4  | 1.354 s | 0.0143 s | 0.0134 s | 1.355 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.386 s | 0.0193 s | 0.0180 s | 1.389 s |
| xUnit     | 2.9.3   | 1.385 s | 0.0188 s | 0.0175 s | 1.384 s |
| MSTest    | 3.10.4  | 1.395 s | 0.0145 s | 0.0136 s | 1.393 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean     | Error    | StdDev    | Median   |
|---------- |-------- |---------:|---------:|----------:|---------:|
| TUnit     | 0.61.39 |       NA |       NA |        NA |       NA |
| NUnit     | 4.4.0   | 904.2 ms | 40.20 ms | 118.54 ms | 892.8 ms |
| xUnit     | 2.9.3   | 999.0 ms | 40.96 ms | 118.84 ms | 977.4 ms |
| MSTest    | 3.10.4  | 796.2 ms | 27.64 ms |  81.05 ms | 794.0 ms |
| TUnit_AOT | 0.61.39 |       NA |       NA |        NA |       NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.270 s | 0.0115 s | 0.0108 s | 1.270 s |
| xUnit     | 2.9.3   | 1.274 s | 0.0193 s | 0.0181 s | 1.276 s |
| MSTest    | 3.10.4  | 1.292 s | 0.0185 s | 0.0173 s | 1.293 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.461 s | 0.0291 s | 0.0663 s | 1.469 s |
| xUnit     | 2.9.3   | 1.497 s | 0.0296 s | 0.0478 s | 1.495 s |
| MSTest    | 3.10.4  | 1.535 s | 0.0302 s | 0.0296 s | 1.535 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit     | 0.61.39 |         NA |       NA |        NA |         NA |
| NUnit     | 4.4.0   | 1,060.4 ms | 45.72 ms | 134.08 ms | 1,055.7 ms |
| xUnit     | 2.9.3   |   870.4 ms | 36.89 ms | 108.19 ms |   844.3 ms |
| MSTest    | 3.10.4  |   799.8 ms | 27.06 ms |  77.63 ms |   808.5 ms |
| TUnit_AOT | 0.61.39 |         NA |       NA |        NA |         NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.213 s | 0.0051 s | 0.0040 s | 1.214 s |
| xUnit     | 2.9.3   | 1.214 s | 0.0060 s | 0.0056 s | 1.214 s |
| MSTest    | 3.10.4  | 1.229 s | 0.0087 s | 0.0077 s | 1.228 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.398 s | 0.0154 s | 0.0144 s | 1.396 s |
| xUnit     | 2.9.3   | 1.403 s | 0.0273 s | 0.0242 s | 1.403 s |
| MSTest    | 3.10.4  | 1.387 s | 0.0127 s | 0.0119 s | 1.390 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.203 s | 0.1215 s | 0.3584 s | 1.155 s |
| xUnit     | 2.9.3   | 1.430 s | 0.1077 s | 0.3125 s | 1.367 s |
| MSTest    | 3.10.4  | 1.361 s | 0.1012 s | 0.2985 s | 1.350 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.214 s | 0.0083 s | 0.0074 s | 1.214 s |
| xUnit     | 2.9.3   | 1.217 s | 0.0119 s | 0.0112 s | 1.213 s |
| MSTest    | 3.10.4  | 1.213 s | 0.0070 s | 0.0066 s | 1.214 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.396 s | 0.0253 s | 0.0301 s | 1.388 s |
| xUnit     | 2.9.3   | 1.395 s | 0.0145 s | 0.0136 s | 1.399 s |
| MSTest    | 3.10.4  | 1.431 s | 0.0263 s | 0.0270 s | 1.424 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean       | Error     | StdDev   | Median     |
|---------- |-------- |-----------:|----------:|---------:|-----------:|
| TUnit     | 0.61.39 |         NA |        NA |       NA |         NA |
| NUnit     | 4.4.0   | 1,540.6 ms | 103.00 ms | 303.7 ms | 1,547.0 ms |
| xUnit     | 2.9.3   | 1,007.1 ms |  53.42 ms | 152.4 ms |   974.5 ms |
| MSTest    | 3.10.4  |   876.4 ms |  38.70 ms | 113.5 ms |   878.2 ms |
| TUnit_AOT | 0.61.39 |         NA |        NA |       NA |         NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.291 s | 0.0097 s | 0.0091 s | 1.293 s |
| xUnit     | 2.9.3   | 1.295 s | 0.0083 s | 0.0078 s | 1.294 s |
| MSTest    | 3.10.4  | 1.285 s | 0.0102 s | 0.0091 s | 1.286 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.705 s | 0.0491 s | 0.1449 s | 1.707 s |
| xUnit     | 2.9.3   | 1.678 s | 0.0398 s | 0.1115 s | 1.642 s |
| MSTest    | 3.10.4  | 1.630 s | 0.0280 s | 0.0311 s | 1.632 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)


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
| Method    | Version | Mean       | Error    | StdDev    | Median     |
|---------- |-------- |-----------:|---------:|----------:|-----------:|
| TUnit     | 0.61.39 |         NA |       NA |        NA |         NA |
| NUnit     | 4.4.0   | 1,019.4 ms | 47.95 ms | 138.35 ms | 1,019.5 ms |
| xUnit     | 2.9.3   | 1,057.6 ms | 67.23 ms | 198.22 ms | 1,033.5 ms |
| MSTest    | 3.10.4  |   815.1 ms | 23.69 ms |  64.84 ms |   806.0 ms |
| TUnit_AOT | 0.61.39 |         NA |       NA |        NA |         NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### ubuntu-latest

```

BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.276 s | 0.0093 s | 0.0087 s | 1.277 s |
| xUnit     | 2.9.3   | 1.278 s | 0.0130 s | 0.0121 s | 1.275 s |
| MSTest    | 3.10.4  | 1.268 s | 0.0073 s | 0.0064 s | 1.269 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



#### windows-latest

```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  Job-YNJDZW : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Runtime=.NET 9.0  

```
| Method    | Version | Mean    | Error    | StdDev   | Median  |
|---------- |-------- |--------:|---------:|---------:|--------:|
| TUnit     | 0.61.39 |      NA |       NA |       NA |      NA |
| NUnit     | 4.4.0   | 1.394 s | 0.0162 s | 0.0144 s | 1.395 s |
| xUnit     | 2.9.3   | 1.406 s | 0.0060 s | 0.0053 s | 1.407 s |
| MSTest    | 3.10.4  | 1.405 s | 0.0134 s | 0.0112 s | 1.407 s |
| TUnit_AOT | 0.61.39 |      NA |       NA |       NA |      NA |

Benchmarks with issues:
  RuntimeBenchmarks.TUnit: Job-YNJDZW(Runtime=.NET 9.0)
  RuntimeBenchmarks.TUnit_AOT: Job-YNJDZW(Runtime=.NET 9.0)



