# TUnit

<a href="https://trendshift.io/repositories/11781" target="_blank"><img src="https://trendshift.io/api/badge/repositories/11781" alt="thomhurst%2FTUnit | Trendshift" style="width: 250px; height: 55px;" width="250" height="55"/></a>

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Trimmed Single File application support included! 

TUnit is designed to aid with all testing types:
- Unit
- Integration
- Acceptance
- and more!


![GitHub Repo stars](https://img.shields.io/github/stars/thomhurst/TUnit) [![GitHub Sponsors](https://img.shields.io/github/sponsors/thomhurst)](https://github.com/sponsors/thomhurst)
 [![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) [![NuGet Downloads](https://img.shields.io/nuget/dt/TUnit)](https://www.nuget.org/packages/TUnit/)
 ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit) 

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

BenchmarkDotNet v0.14.0, macOS Sonoma 14.7 (23H124) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method       | Mean     | Error    | StdDev   |
|------------- |---------:|---------:|---------:|
| Build_TUnit  | 931.6 ms | 18.02 ms | 26.42 ms |
| Build_NUnit  | 795.1 ms | 10.16 ms |  8.48 ms |
| Build_xUnit  | 816.3 ms | 15.10 ms | 17.98 ms |
| Build_MSTest | 835.7 ms | 15.01 ms | 17.87 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.793 s | 0.0351 s | 0.0329 s |
| Build_NUnit  | 1.491 s | 0.0137 s | 0.0128 s |
| Build_xUnit  | 1.506 s | 0.0244 s | 0.0228 s |
| Build_MSTest | 1.568 s | 0.0189 s | 0.0176 s |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2762) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method       | Mean    | Error    | StdDev   |
|------------- |--------:|---------:|---------:|
| Build_TUnit  | 1.944 s | 0.0380 s | 0.0494 s |
| Build_NUnit  | 1.645 s | 0.0187 s | 0.0165 s |
| Build_xUnit  | 1.690 s | 0.0336 s | 0.0330 s |
| Build_MSTest | 1.694 s | 0.0228 s | 0.0214 s |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.7 (23H124) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean     | Error    | StdDev   |
|---------- |---------:|---------:|---------:|
| TUnit_AOT | 121.7 ms |  0.60 ms |  0.53 ms |
| TUnit     | 460.5 ms |  7.75 ms |  7.25 ms |
| NUnit     | 710.3 ms | 10.82 ms |  9.04 ms |
| xUnit     | 681.8 ms | 13.14 ms | 12.90 ms |
| MSTest    | 627.5 ms | 11.22 ms | 10.50 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    67.04 ms |  1.330 ms |  2.657 ms |
| TUnit     |   823.49 ms | 16.408 ms | 21.335 ms |
| NUnit     | 1,319.27 ms | 17.330 ms | 16.211 ms |
| xUnit     | 1,286.93 ms | 15.035 ms | 14.064 ms |
| MSTest    | 1,165.93 ms | 23.188 ms | 20.556 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2762) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   105.8 ms |  2.22 ms |  6.55 ms |
| TUnit     |   890.0 ms | 17.77 ms | 24.91 ms |
| NUnit     | 1,361.8 ms | 19.71 ms | 18.43 ms |
| xUnit     | 1,342.4 ms | 20.50 ms | 19.18 ms |
| MSTest    | 1,222.3 ms | 13.85 ms | 12.95 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.7 (23H124) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    296.8 ms |  14.37 ms |  42.37 ms |
| TUnit     |    640.9 ms |  21.64 ms |  63.47 ms |
| NUnit     | 14,242.3 ms | 283.13 ms | 545.49 ms |
| xUnit     | 14,348.7 ms | 282.07 ms | 522.84 ms |
| MSTest    | 14,447.7 ms | 286.70 ms | 462.97 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   130.7 ms |  2.67 ms |  7.88 ms |
| TUnit     |   902.9 ms | 17.88 ms | 20.59 ms |
| NUnit     | 6,487.9 ms | 17.69 ms | 15.69 ms |
| xUnit     | 6,448.4 ms | 17.29 ms | 15.32 ms |
| MSTest    | 6,419.4 ms | 16.20 ms | 13.53 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2762) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   174.8 ms |  3.45 ms |  8.27 ms |
| TUnit     |   957.0 ms | 19.13 ms | 23.49 ms |
| NUnit     | 7,574.2 ms | 14.71 ms | 13.04 ms |
| xUnit     | 7,534.8 ms | 23.61 ms | 22.09 ms |
| MSTest    | 7,502.3 ms | 18.28 ms | 17.10 ms |



