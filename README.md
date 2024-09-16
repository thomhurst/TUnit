# TUnit

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Trimmed Single File application support included!


[![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) ![Nuget](https://img.shields.io/nuget/dt/TUnit) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit) 

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## IDE

TUnit is built on top of the newer Microsoft.Testing.Platform, as opposed to the older VSTest platform. As of September 2024, IDEs do not fully support this testing platform yet.

Visual Studio 17.10 onwards can run the new tests by enabling the new testing platform server mode, within Visual Studio preview/experimental features. You will have to opt in to this manually.

For Rider, it is not yet supported. I believe they are working on it so we just have to wait for now.

`dotnet` CLI - Fully supported. Tests should be runnable with `dotnet test`, `dotnet run`, `dotnet exec` or executing an executable directly. See the docs for more information!

## Features

- Native AOT / Trimmed Single File application support
- Source generated tests
- Dependency injection support ([See here](https://thomhurst.github.io/TUnit/docs/tutorial-extras/class-constructors))
- Full async support
- Parallel by default, with mechanisms to:
    - Run specific tests completely on their own
    - Run specific tests not in parallel with other specific tests
    - Limit the parallel limit on a per-test, class or assembly level
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

        await Assert.That(value).IsNotNull()
                .And.IsEqualTo("hello world!", StringComparison.InvariantCultureIgnoreCase);
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

It aims to build upon the useful features of both while trying to address any pain points that they may have.

[Read more here](https://thomhurst.github.io/TUnit/docs/comparison/framework-differences)

## Benchmark

### Scenario: Building the test project

#### markdown

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.587 s | 0.0304 s | 0.0395 s |
| Build_NUnit  | 1.461 s | 0.0277 s | 0.0272 s |
| Build_xUnit  | 1.439 s | 0.0278 s | 0.0320 s |
| Build_MSTest | 1.463 s | 0.0246 s | 0.0230 s |


### Scenario: Building the test project

#### markdown

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.385 s | 0.0196 s | 0.0164 s |
| Build_NUnit  | 1.289 s | 0.0134 s | 0.0125 s |
| Build_xUnit  | 1.302 s | 0.0167 s | 0.0156 s |
| Build_MSTest | 1.315 s | 0.0178 s | 0.0166 s |


### Scenario: Building the test project

#### markdown

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method       | Mean       | Error    | StdDev    |
|------------- |-----------:|---------:|----------:|
| Build_TUnit  | 1,426.3 ms | 75.95 ms | 223.95 ms |
| Build_NUnit  |   995.3 ms | 38.36 ms | 111.90 ms |
| Build_xUnit  |   865.5 ms | 27.18 ms |  79.72 ms |
| Build_MSTest |   902.0 ms | 29.98 ms |  87.46 ms |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method    | Mean      | Error     | StdDev    | Median    |
|---------- |----------:|----------:|----------:|----------:|
| TUnit_AOT |  98.52 ms |  7.139 ms | 20.826 ms |  88.38 ms |
| TUnit     | 475.99 ms |  9.457 ms | 16.810 ms | 474.36 ms |
| NUnit     | 756.02 ms | 24.318 ms | 71.319 ms | 748.29 ms |
| xUnit     | 681.62 ms |  7.755 ms |  6.476 ms | 681.12 ms |
| MSTest    | 612.59 ms |  7.752 ms |  7.251 ms | 611.72 ms |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    | Median      |
|---------- |------------:|----------:|----------:|------------:|
| TUnit_AOT |    78.00 ms |  0.127 ms |  0.106 ms |    77.99 ms |
| TUnit     |   749.19 ms | 14.852 ms | 23.983 ms |   737.39 ms |
| NUnit     | 1,285.43 ms |  6.357 ms |  5.946 ms | 1,286.08 ms |
| xUnit     | 1,277.88 ms | 11.588 ms | 10.273 ms | 1,277.22 ms |
| MSTest    | 1,148.88 ms | 10.159 ms |  9.006 ms | 1,146.31 ms |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    | Median      |
|---------- |------------:|----------:|----------:|------------:|
| TUnit_AOT |    44.13 ms |  1.065 ms |  3.141 ms |    44.33 ms |
| TUnit     |   807.89 ms | 16.078 ms | 42.355 ms |   795.14 ms |
| NUnit     | 1,415.97 ms | 17.993 ms | 15.951 ms | 1,414.23 ms |
| xUnit     | 1,422.47 ms | 14.526 ms | 13.588 ms | 1,424.60 ms |
| MSTest    | 1,277.73 ms | 12.561 ms | 11.135 ms | 1,274.86 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    | Median      |
|---------- |------------:|----------:|----------:|------------:|
| TUnit_AOT |    92.54 ms |  1.955 ms |  5.766 ms |    92.76 ms |
| TUnit     |   858.73 ms | 17.058 ms | 38.504 ms |   842.38 ms |
| NUnit     | 6,544.04 ms | 16.562 ms | 15.492 ms | 6,549.49 ms |
| xUnit     | 6,596.58 ms | 23.693 ms | 22.162 ms | 6,600.91 ms |
| MSTest    | 6,517.66 ms | 26.946 ms | 25.205 ms | 6,520.67 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean       | Error     | StdDev    | Median     |
|---------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT |   139.1 ms |   2.78 ms |   6.44 ms |   140.4 ms |
| TUnit     |   838.7 ms |  16.39 ms |  23.50 ms |   840.2 ms |
| NUnit     | 8,664.9 ms | 172.02 ms | 402.08 ms | 8,824.6 ms |
| xUnit     | 8,728.1 ms | 170.87 ms | 266.03 ms | 8,792.9 ms |
| MSTest    | 8,619.0 ms | 171.29 ms | 376.00 ms | 8,765.3 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### markdown

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    246.8 ms |  11.29 ms |  33.30 ms |
| TUnit     |    647.3 ms |  22.67 ms |  66.49 ms |
| NUnit     | 14,221.8 ms | 282.66 ms | 620.45 ms |
| xUnit     | 14,245.5 ms | 281.53 ms | 507.66 ms |
| MSTest    | 14,346.3 ms | 279.97 ms | 427.55 ms |



