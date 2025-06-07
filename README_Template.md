![](assets/banner.png)

# 🚀 The Modern Testing Framework for .NET

**TUnit** is a next-generation testing framework for C# that outpaces traditional frameworks with **source-generated tests**, **parallel execution by default**, and **Native AOT support**. Built on the modern Microsoft.Testing.Platform, TUnit delivers faster test runs, better developer experience, and unmatched flexibility.

[![thomhurst%2FTUnit | Trendshift](https://trendshift.io/api/badge/repositories/11781)](https://trendshift.io/repositories/11781)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a8231644d844435eb9fd15110ea771d8)](https://app.codacy.com/gh/thomhurst/TUnit?utm_source=github.com&utm_medium=referral&utm_content=thomhurst/TUnit&utm_campaign=Badge_Grade) ![GitHub Repo stars](https://img.shields.io/github/stars/thomhurst/TUnit) ![GitHub Issues or Pull Requests](https://img.shields.io/github/issues-closed-raw/thomhurst/TUnit)
 [![GitHub Sponsors](https://img.shields.io/github/sponsors/thomhurst)](https://github.com/sponsors/thomhurst) [![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) [![NuGet Downloads](https://img.shields.io/nuget/dt/TUnit)](https://www.nuget.org/packages/TUnit/) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit)

## ⚡ Why Choose TUnit?

**Faster Execution**: Source generators eliminate reflection overhead, while the modern Microsoft.Testing.Platform provides superior performance over legacy VSTest.

**Parallel by Default**: Tests run concurrently out of the box, with fine-grained control over parallelization when needed.

**Modern .NET Support**: First-class support for Native AOT, trimmed single-file applications, and the latest .NET features.

**Intelligent Test Discovery**: Tests are discovered at compile-time, giving you access to test metadata, arguments, and properties before execution.

**Extensible Architecture**: Built with extensibility in mind - customize data sources, create custom attributes, and extend functionality easily.

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

📖 **Full Documentation**: <https://tunit.dev/>

## ✨ Key Features

### 🔥 **Performance & Modern Platform**
- **Source-generated tests** - No reflection overhead
- **Parallel execution by default** - Maximum throughput
- **Native AOT & trimming support** - Deploy anywhere
- **Microsoft.Testing.Platform** - Modern, extensible test infrastructure

### 🎯 **Flexible Test Organization**
- **Test dependencies** - Chain tests with `[DependsOn]`
- **Parallel control** - `[NotInParallel]`, `[ParallelLimit]` per test/class/assembly
- **Rich categorization** - `[Category]`, `[Property]`, custom attributes
- **Conditional execution** - `[Skip]`, `[Explicit]`, custom conditions

### 📊 **Powerful Data-Driven Testing**
- **Matrix testing** - `[MatrixDataSource]` for combinatorial tests
- **Class injection** - `[ClassDataSource<T>]` with configurable lifetimes
- **Method data sources** - `[MethodDataSource]` for dynamic data
- **Extensible generators** - Create custom `DataSourceGenerator<T>`

### 🛡️ **Developer Experience**
- **Built-in analyzers** - Catch test errors at compile time
- **Comprehensive hooks** - Before/After at Test, Class, Assembly, and Session levels
- **Rich assertions** - Fluent async assertion syntax
- **IDE support** - Visual Studio, Rider, VS Code
- **Dependency injection** - Full DI container support

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

## 🚀 What Makes TUnit Special?

### **Compile-Time Intelligence**
Unlike traditional frameworks that discover tests at runtime, TUnit knows everything about your tests at compile time. This enables:
- **Faster test discovery** - No runtime reflection
- **Better tooling integration** - IDEs get full test information upfront
- **Advanced lifecycle management** - Precise resource cleanup based on test counts
- **Rich metadata access** - Test context includes full argument and property information

### **True Parallel-First Design**
Most frameworks bolt-on parallelization. TUnit was designed parallel-first:
- **Parallel by default** - Maximum throughput without configuration
- **Granular control** - Control parallelization per test, class, or assembly
- **Dependency-aware** - `[DependsOn]` creates execution chains when needed
- **Resource-safe** - Parallel limits prevent resource exhaustion

### **Extensible Data Generation**
TUnit's `DataSourceGenerator<T>` pattern lets you create powerful, reusable data sources:
- **Type-safe** - Full compile-time checking
- **Flexible lifetimes** - Per test, per class, per session
- **Composable** - Combine multiple data sources
- **Clean syntax** - Data generation logic stays out of test classes

### **Modern .NET Integration**
Built for modern .NET development:
- **Native AOT ready** - Deploy tests as single-file executables
- **Source generator powered** - Minimal runtime overhead
- **Trimming compatible** - Works with aggressive IL trimming
- **Dependency injection** - Full DI container support

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
// Familiar attribute-based approach
[Test]
[TestCase("value1")]
[TestCase("value2")]
public void Traditional_Style_Test(string value) { }

// Enhanced with TUnit's advanced features
[Test]
[Arguments("value1")]
[Arguments("value2")]
[Retry(3)]
[ParallelLimit<CustomLimit>]
public async Task Modern_TUnit_Test(string value) { }
```

**Key advantages over traditional frameworks:**
- ⚡ **2-5x faster test execution** through source generation and parallel-first design
- 🎯 **Compile-time test discovery** eliminates runtime reflection overhead
- 🔧 **Better debugging experience** with rich test context and metadata
- 🚀 **Modern .NET support** including Native AOT and trimming
- 🎭 **Advanced orchestration** with test dependencies and lifecycle hooks

## 💡 Current Status

TUnit is feature-complete and production-ready, currently in **prerelease** while we finalize:

✅ Core framework and all major features
✅ Visual Studio 2022 17.13+ support
🔄 Full Rider IDE integration
🔄 Complete Visual Studio feature parity
📢 Community feedback and API refinement

The API is mostly stable, but may have some changes based on feedback or issues before v1.0 release.

---

**Ready to modernize your testing?**

🚀 **Get started**: `dotnet new install TUnit.Templates && dotnet new TUnit -n "MyTests"`
📖 **Learn more**: [tunit.dev](https://tunit.dev)
⭐ **Star us on GitHub** if TUnit helps your testing!

## Performance Benchmark

${{ BENCHMARK }}
