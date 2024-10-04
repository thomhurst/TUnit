# TUnit

<a href="https://trendshift.io/repositories/11781" target="_blank"><img src="https://trendshift.io/api/badge/repositories/11781" alt="thomhurst%2FTUnit | Trendshift" style="width: 250px; height: 55px;" width="250" height="55"/></a>

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Trimmed Single File application support included! 

TUnit is designed to aid with all testing types:
- Unit
- Integration
- Acceptance
- and more!


[![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) ![Nuget](https://img.shields.io/nuget/dt/TUnit) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit) 

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## IDE

TUnit is built on top of the newer Microsoft.Testing.Platform, as opposed to the older VSTest platform. Because the infrastructure behind the scenes is new and different, you may need to enable some settings. This should just be a one time thing.

### Visual Studio

Visual Studio is supported on the Preview version currently. 

- Install the [latest preview version](https://visualstudio.microsoft.com/vs/preview/)
- Open Visual Studio and go to Tools > Manage Preview Features
- Enable "Use testing platform server mode"

<img src="/docs/static/img/visual-studio.png" height="300px">

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
- Property injection
- Full async support
- Parallel by default, with mechanisms to:
    - Run specific tests completely on their own
    - Run specific tests not in parallel with other specific tests
    - Limit the parallel limit on a per-test, class or assembly level
- Tests can depend on other tests to form chains, useful for if one test depends on state from another action. While not recommended for unit tests, this can be useful in integration testing where state matters
- Easy to read assertions - though you're also free to use whichever assertion library you like
- Injectable test data via classes, methods, compile-time args, or matrices
- Hooks before and after: 
    - TestDiscover
    - TestSession
    - Assembly
    - Class
    - Test
- Designed to avoid common pitfalls such as leaky test states
- Dependency injection support ([See here](https://thomhurst.github.io/TUnit/docs/tutorial-extras/class-constructors))
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

## Prerelease

You'll notice that version 1.0 isn't out yet. While this framework is mostly feature complete, I'm waiting for a few things:

- Full Rider support for all features
- Full VS support for all features
- Open to feedback on existing features
- Open to ideas on new features

As such, the API may change. I'll try to limit this but it's a possibility.

## Benchmark

### Scenario: Building the test project

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method       | Mean       | Error    | StdDev    |
|------------- |-----------:|---------:|----------:|
| Build_TUnit  | 1,164.1 ms | 57.47 ms | 164.89 ms |
| Build_NUnit  |   956.2 ms | 34.40 ms | 101.43 ms |
| Build_xUnit  |   893.0 ms | 19.44 ms |  57.33 ms |
| Build_MSTest |   994.7 ms | 34.08 ms |  99.96 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.740 s | 0.0278 s | 0.0260 s |
| Build_NUnit  | 1.573 s | 0.0208 s | 0.0184 s |
| Build_xUnit  | 1.583 s | 0.0224 s | 0.0199 s |
| Build_MSTest | 1.653 s | 0.0181 s | 0.0169 s |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.612 s | 0.0221 s | 0.0196 s |
| Build_NUnit  | 1.484 s | 0.0212 s | 0.0198 s |
| Build_xUnit  | 1.501 s | 0.0129 s | 0.0121 s |
| Build_MSTest | 1.529 s | 0.0106 s | 0.0083 s |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean       | Error    | StdDev    | Median     |
|---------- |-----------:|---------:|----------:|-----------:|
| TUnit_AOT |   121.8 ms |  9.93 ms |  29.13 ms |   109.0 ms |
| TUnit     |   651.9 ms | 22.13 ms |  63.15 ms |   643.9 ms |
| NUnit     | 1,037.9 ms | 51.70 ms | 152.43 ms | 1,011.5 ms |
| xUnit     | 1,022.3 ms | 35.98 ms | 104.39 ms |   996.8 ms |
| MSTest    |   925.8 ms | 30.56 ms |  87.69 ms |   916.1 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    47.70 ms |  0.950 ms |  1.875 ms |
| TUnit     |   747.41 ms | 14.551 ms | 15.570 ms |
| NUnit     | 1,317.08 ms | 14.719 ms | 13.768 ms |
| xUnit     | 1,306.01 ms |  7.265 ms |  6.440 ms |
| MSTest    | 1,202.62 ms | 15.795 ms | 14.774 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    77.83 ms |  0.731 ms |  0.611 ms |
| TUnit     |   766.98 ms | 15.215 ms | 21.821 ms |
| NUnit     | 1,326.09 ms | 16.921 ms | 15.000 ms |
| xUnit     | 1,316.71 ms | 14.323 ms | 13.398 ms |
| MSTest    | 1,194.94 ms | 17.915 ms | 14.960 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    242.4 ms |   9.95 ms |  29.17 ms |
| TUnit     |    638.9 ms |  28.84 ms |  85.03 ms |
| NUnit     | 14,119.3 ms | 278.22 ms | 580.75 ms |
| xUnit     | 14,207.9 ms | 269.96 ms | 507.05 ms |
| MSTest    | 14,604.3 ms | 289.62 ms | 715.86 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    86.05 ms |  1.711 ms |  4.826 ms |
| TUnit     |   828.72 ms | 15.538 ms | 17.271 ms |
| NUnit     | 6,300.42 ms | 14.038 ms | 11.722 ms |
| xUnit     | 6,376.86 ms | 19.495 ms | 17.282 ms |
| MSTest    | 6,293.48 ms | 15.929 ms | 14.900 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.1.24452.12
  [Host]   : .NET 9.0.0 (9.0.24.43107), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   138.4 ms |  2.75 ms |  6.75 ms |
| TUnit     |   916.2 ms | 18.29 ms | 30.56 ms |
| NUnit     | 7,634.8 ms | 34.35 ms | 32.13 ms |
| xUnit     | 7,621.5 ms | 28.68 ms | 26.83 ms |
| MSTest    | 7,589.1 ms | 34.24 ms | 32.03 ms |



