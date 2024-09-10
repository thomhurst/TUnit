# TUnit

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Single File application support included!

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## IDE

TUnit is built on top of the newer Microsoft.Testing.Platform, as opposed to the older VSTest platform. As of September 2024, IDEs do not fully support this testing platform yet.

Visual Studio 17.10 onwards can run the new tests by enabling the new testing platform server mode, within Visual Studio preview/experimental features. You will have to opt in to this manually.

For Rider, it is not yet supported. I believe they are working on it so we just have to wait for now.

`dotnet` CLI - Fully supported. Tests should be runnable with `dotnet test` or `dotnet run`, `dotnet exec` or executing an executable directly. See the docs for more information!

## Features

- Native AOT + Single File application support
- Source generated tests
- Full async support
- Parallel by default, with mechanisms to:
    - Run specific tests completely on their own
    - Run specific tests not in parallel with other specific tests
    - Limit the a parallel limit on a per-test, class or assembly level
- Test ordering (if running not in parallel)
- Tests can depend on other tests to form chains, useful for if one test depends on state from another action
- Easy to read assertions
- Injectable test data via classes, methods, compile-time args, or matrices
- Hooks before and after: 
    - TestDiscover
    - TestSession
    - Assembly
    - Class
    - Test
- Designed to avoid common pitfalls such as leaky test states
- Ability to view and interrogate metadata and results from various assembly/class/test context objects

## Installation

`dotnet add package TUnit --prerelease`

## Example test

```csharp
    [Test]
    public async Task Test1()
    {
        var value = "Hello world!";

        await Assert.That(value)
            .Is.Not.Null
            .And.Does.StartWith("H")
            .And.Has.Count().EqualTo(12)
            .And.Is.EqualTo("hello world!", StringComparison.InvariantCultureIgnoreCase);
    }
```

or with more complex test orchestration needs

```csharp
    [Before(Class)]
    public static async Task ClearDatabase(ClassHookContext context) { ... }

    [After(Class)]
    public static async Task AssertDatabaseIsAsExpected(ClassHookContext context) { ... }

    [Before(Test)]
    public async Task CreatePlaywrightBrowser(TestContext context) { ... }

    [After(Test)]
    public async Task DisposePlaywrightBrowser(TestContext context) { ... }

    [Retry(3)]
    [Test, DisplayName("Register an account")]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task Register(string username, string password) { ... }

    [Repeat(5)]
    [Test, DependsOn(nameof(Register))]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task Login(string username, string password) { ... }

    [Test, DependsOn(nameof(Login), [typeof(string), typeof(string)])]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task DeleteAccount(string username, string password) { ... }

    [Category("Downloads")]
    [Timeout(300_000)]
    [Test, NotInParallel(Order = 1)]
    public async Task DownloadFile1() { ... }

    [Category("Downloads")]
    [Timeout(300_000)]
    [Test, NotInParallel(Order = 2)]
    public async Task DownloadFile2() { ... }

    [Repeat(10)]
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [DisplayName("Go to the page numbered $page")]
    public async Task GoToPage(int page) { ... }

    [Category("Cookies")]
    [Test, Skip("Not yet built!")]
    public async Task CheckCookies() { ... }

    [Test, Explicit, WindowsOnlyTest, RetryHttpServiceUnavailable(5)]
    [Property("Some Key", "Some Value")]
    public async Task Ping() { ... }

    [Test]
    [ParallelLimit<LoadTestParallelLimit>]
    [Repeat(1000)]
    public async Task LoadHomepage() { ... }

    public static IEnumerable<(string Username, string Password)> GetAuthDetails()
    {
        yield return ("user1", "password1");
        yield return ("user2", "password2");
        yield return ("user3", "password3");
    }

    public class WindowsOnlyTestAttribute : SkipAttribute
    {
        public WindowsOnlyTestAttribute() : base("Windows only test")
        {
        }

        public override Task<bool> ShouldSkip(TestContext testContext)
        {
            return Task.FromResult(!OperatingSystem.IsWindows());
        }
    }

    public class RetryHttpServiceUnavailableAttribute : RetryAttribute
    {
        public RetryHttpServiceUnavailableAttribute(int times) : base(times)
        {
        }

        public override Task<bool> ShouldRetry(TestInformation testInformation, Exception exception, int currentRetryCount)
        {
            return Task.FromResult(exception is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable });
        }
    }

    public class LoadTestParallelLimit : IParallelLimit
    {
        public int Limit => 50;
    }
```

## Motivations

TUnit is inspired by NUnit and xUnit - two of the most popular testing frameworks for .NET.

It aims to build upon the useful features of both while trying to address any pain points that they may have. You may have experienced these, or you may have not even known about them.

[Read more here](https://thomhurst.github.io/TUnit/docs/comparison/framework-differences)

## Benchmark

### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    91.23 ms |  1.817 ms |  2.231 ms |
| TUnit_SingleFile |   416.28 ms |  7.249 ms |  6.053 ms |
| TUnit            |   780.81 ms | 15.591 ms | 18.560 ms |
| NUnit            | 1,318.08 ms | 10.782 ms | 10.086 ms |
| xUnit            | 1,304.45 ms |  9.076 ms |  8.490 ms |
| MSTest           | 1,173.92 ms |  7.018 ms |  6.221 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    33.41 ms |  0.844 ms |  2.462 ms |
| TUnit_SingleFile |   437.06 ms |  7.994 ms |  7.478 ms |
| TUnit            |   818.49 ms | 16.293 ms | 36.104 ms |
| NUnit            | 1,399.20 ms | 25.387 ms | 22.505 ms |
| xUnit            | 1,389.05 ms | 26.991 ms | 26.509 ms |
| MSTest           | 1,241.96 ms | 24.553 ms | 21.766 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method           | Mean       | Error    | StdDev    | Median      |
|----------------- |-----------:|---------:|----------:|------------:|
| TUnit_AOT        |   101.6 ms |  4.43 ms |  12.57 ms |    96.03 ms |
| TUnit_SingleFile | 1,375.2 ms | 64.90 ms | 188.29 ms | 1,312.65 ms |
| TUnit            |   520.9 ms | 10.21 ms |  15.59 ms |   516.25 ms |
| NUnit            |   878.9 ms | 17.22 ms |  21.78 ms |   878.07 ms |
| xUnit            |   841.8 ms | 16.58 ms |  17.74 ms |   840.65 ms |
| MSTest           |   763.1 ms | 13.80 ms |  12.23 ms |   761.21 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    92.88 ms |  1.831 ms |  4.353 ms |
| TUnit_SingleFile |   491.70 ms |  4.922 ms |  4.364 ms |
| TUnit            |   873.74 ms | 17.249 ms | 36.384 ms |
| NUnit            | 6,570.28 ms | 45.924 ms | 42.958 ms |
| xUnit            | 6,554.86 ms | 29.323 ms | 27.429 ms |
| MSTest           | 6,492.26 ms | 21.068 ms | 18.676 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean       | Error    | StdDev   | Median     |
|----------------- |-----------:|---------:|---------:|-----------:|
| TUnit_AOT        |   147.9 ms |  2.93 ms |  5.28 ms |   148.3 ms |
| TUnit_SingleFile |   492.3 ms |  9.73 ms | 13.32 ms |   483.9 ms |
| TUnit            |   838.9 ms | 16.24 ms | 19.94 ms |   838.6 ms |
| NUnit            | 7,513.8 ms | 12.12 ms | 10.74 ms | 7,516.3 ms |
| xUnit            | 7,493.3 ms | 14.61 ms | 13.67 ms | 7,494.5 ms |
| MSTest           | 7,456.5 ms |  9.14 ms |  8.55 ms | 7,456.9 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    245.5 ms |  17.46 ms |  50.65 ms |
| TUnit_SingleFile |  1,557.7 ms |  58.53 ms | 171.65 ms |
| TUnit            |    694.9 ms |  23.62 ms |  69.27 ms |
| NUnit            | 14,034.2 ms | 275.83 ms | 475.78 ms |
| xUnit            | 14,485.2 ms | 288.56 ms | 562.81 ms |
| MSTest           | 14,143.9 ms | 281.87 ms | 478.63 ms |


