# TUnit

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Trimmed Single File application support included!


[![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) ![Nuget](https://img.shields.io/nuget/dt/TUnit) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit) 

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## IDE

TUnit is built on top of the newer Microsoft.Testing.Platform, as opposed to the older VSTest platform. Because the infrastructure behind the scenes is new and different, you may need to enable some settings. This should just be a one time thing.

### Visual Studio

Visual Studio is supported.

### Rider

Rider is supported. The [Enable Testing Platform support](https://www.jetbrains.com/help/rider/Reference__Options__Tools__Unit_Testing__VSTest.html) option must be selected in Settings > Build, Execution, Deployment > Unit Testing > VSTest.

<img src="/docs/static/img/rider.png" height="300px">

## VS Code
Visual Studio Code is supported.

- Install the extension Name: [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- Go to the C# Dev Kit extension's settings
- Enable Dotnet > Test Window > Use Testing Platform Protocol

<img src="/docs/static/img/visual-studio-code.png" height="300px">

### CLI
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
    private static readonly TimeOnly Midnight = TimeOnly.FromTimeSpan(TimeSpan.Zero);
    private static readonly TimeOnly Noon = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12));
    
    [Test]
    public async Task IsMorning()
    {
        var time = GetTime();

        await Assert.That(time).IsAfterOrEqualTo(Midnight)
            .And.IsBefore(Noon);
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
    [MethodData(nameof(GetAuthDetails))]
    public async Task Register(string username, string password) { ... }

    [Repeat(5)]
    [Test, DependsOn(nameof(Register))]
    [MethodData(nameof(GetAuthDetails))]
    public async Task Login(string username, string password) { ... }

    [Test, DependsOn(nameof(Login), [typeof(string), typeof(string)])]
    [MethodData(nameof(GetAuthDetails))]
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

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method       | Mean     | Error    | StdDev   |
|------------- |---------:|---------:|---------:|
| Build_TUnit  | 787.0 ms | 11.19 ms | 16.75 ms |
| Build_NUnit  | 710.3 ms | 13.55 ms | 15.60 ms |
| Build_xUnit  | 704.4 ms |  9.88 ms |  9.24 ms |
| Build_MSTest | 754.4 ms | 12.89 ms | 12.06 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.517 s | 0.0230 s | 0.0216 s |
| Build_NUnit  | 1.383 s | 0.0267 s | 0.0237 s |
| Build_xUnit  | 1.400 s | 0.0198 s | 0.0185 s |
| Build_MSTest | 1.418 s | 0.0216 s | 0.0202 s |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.410 s | 0.0275 s | 0.0376 s |
| Build_NUnit  | 1.290 s | 0.0106 s | 0.0083 s |
| Build_xUnit  | 1.307 s | 0.0256 s | 0.0252 s |
| Build_MSTest | 1.322 s | 0.0108 s | 0.0084 s |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    45.52 ms |  1.123 ms |  3.312 ms |
| TUnit     |   731.72 ms | 14.486 ms | 19.338 ms |
| NUnit     | 1,311.58 ms | 11.782 ms |  9.839 ms |
| xUnit     | 1,298.54 ms | 14.451 ms | 13.518 ms |
| MSTest    | 1,191.60 ms | 14.747 ms | 13.794 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    79.57 ms |  1.582 ms |  1.758 ms |
| TUnit     |   760.44 ms | 15.071 ms | 20.120 ms |
| NUnit     | 1,319.92 ms | 15.638 ms | 14.628 ms |
| xUnit     | 1,308.98 ms | 22.597 ms | 21.138 ms |
| MSTest    | 1,185.25 ms | 16.841 ms | 15.753 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method    | Mean      | Error     | StdDev     | Median    |
|---------- |----------:|----------:|-----------:|----------:|
| TUnit_AOT |  93.11 ms |  5.657 ms |  16.502 ms |  86.33 ms |
| TUnit     | 421.62 ms |  7.932 ms |  20.616 ms | 412.31 ms |
| NUnit     | 689.12 ms |  7.113 ms |   6.654 ms | 689.90 ms |
| xUnit     | 887.74 ms | 39.790 ms | 115.438 ms | 924.08 ms |
| MSTest    | 664.78 ms | 12.946 ms |  19.770 ms | 667.13 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    91.40 ms |  1.821 ms |  5.047 ms |
| TUnit     |   831.20 ms | 16.107 ms | 26.011 ms |
| NUnit     | 6,343.54 ms |  8.060 ms |  7.539 ms |
| xUnit     | 6,424.20 ms | 14.931 ms | 13.236 ms |
| MSTest    | 6,336.65 ms | 21.178 ms | 19.810 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   134.3 ms |  2.68 ms |  6.52 ms |
| TUnit     |   827.7 ms | 16.36 ms | 20.09 ms |
| NUnit     | 7,494.2 ms | 18.08 ms | 16.91 ms |
| xUnit     | 7,478.1 ms | 18.89 ms | 16.74 ms |
| MSTest    | 7,455.6 ms | 22.37 ms | 20.93 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    251.0 ms |  15.49 ms |  45.66 ms |
| TUnit     |    673.0 ms |  21.68 ms |  63.93 ms |
| NUnit     | 14,295.6 ms | 284.12 ms | 526.64 ms |
| xUnit     | 14,482.0 ms | 285.39 ms | 460.85 ms |
| MSTest    | 14,455.9 ms | 286.31 ms | 537.77 ms |



