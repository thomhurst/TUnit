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
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD


```
| Method       | Job      | Runtime  | Mean     | Error    | StdDev    | Median   |
|------------- |--------- |--------- |---------:|---------:|----------:|---------:|
| Build_TUnit  | .NET 8.0 | .NET 8.0 | 897.3 ms | 17.82 ms |  24.40 ms | 895.7 ms |
| Build_NUnit  | .NET 8.0 | .NET 8.0 | 811.7 ms | 14.97 ms |  13.27 ms | 811.5 ms |
| Build_xUnit  | .NET 8.0 | .NET 8.0 | 810.7 ms | 15.23 ms |  12.72 ms | 810.0 ms |
| Build_MSTest | .NET 8.0 | .NET 8.0 | 837.0 ms | 12.77 ms |  11.32 ms | 838.5 ms |
| Build_TUnit  | .NET 9.0 | .NET 9.0 | 886.8 ms | 17.55 ms |  30.27 ms | 873.2 ms |
| Build_NUnit  | .NET 9.0 | .NET 9.0 | 864.5 ms | 17.22 ms |  48.57 ms | 850.5 ms |
| Build_xUnit  | .NET 9.0 | .NET 9.0 | 961.9 ms | 55.31 ms | 155.11 ms | 919.2 ms |
| Build_MSTest | .NET 9.0 | .NET 9.0 | 875.1 ms | 17.45 ms |  27.16 ms | 873.4 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method       | Job      | Runtime  | Mean    | Error    | StdDev   |
|------------- |--------- |--------- |--------:|---------:|---------:|
| Build_TUnit  | .NET 8.0 | .NET 8.0 | 1.743 s | 0.0341 s | 0.0393 s |
| Build_NUnit  | .NET 8.0 | .NET 8.0 | 1.564 s | 0.0291 s | 0.0258 s |
| Build_xUnit  | .NET 8.0 | .NET 8.0 | 1.579 s | 0.0304 s | 0.0285 s |
| Build_MSTest | .NET 8.0 | .NET 8.0 | 1.640 s | 0.0296 s | 0.0262 s |
| Build_TUnit  | .NET 9.0 | .NET 9.0 | 1.730 s | 0.0296 s | 0.0262 s |
| Build_NUnit  | .NET 9.0 | .NET 9.0 | 1.575 s | 0.0302 s | 0.0252 s |
| Build_xUnit  | .NET 9.0 | .NET 9.0 | 1.536 s | 0.0270 s | 0.0341 s |
| Build_MSTest | .NET 9.0 | .NET 9.0 | 1.599 s | 0.0171 s | 0.0133 s |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method       | Job      | Runtime  | Mean    | Error    | StdDev   |
|------------- |--------- |--------- |--------:|---------:|---------:|
| Build_TUnit  | .NET 8.0 | .NET 8.0 | 1.682 s | 0.0231 s | 0.0205 s |
| Build_NUnit  | .NET 8.0 | .NET 8.0 | 1.515 s | 0.0274 s | 0.0257 s |
| Build_xUnit  | .NET 8.0 | .NET 8.0 | 1.523 s | 0.0178 s | 0.0158 s |
| Build_MSTest | .NET 8.0 | .NET 8.0 | 1.582 s | 0.0188 s | 0.0176 s |
| Build_TUnit  | .NET 9.0 | .NET 9.0 | 1.699 s | 0.0323 s | 0.0302 s |
| Build_NUnit  | .NET 9.0 | .NET 9.0 | 1.552 s | 0.0255 s | 0.0238 s |
| Build_xUnit  | .NET 9.0 | .NET 9.0 | 1.560 s | 0.0192 s | 0.0180 s |
| Build_MSTest | .NET 9.0 | .NET 9.0 | 1.596 s | 0.0197 s | 0.0175 s |


### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.7 (23H124) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD


```
| Method    | Job      | Runtime  | Mean     | Error    | StdDev    | Median   |
|---------- |--------- |--------- |---------:|---------:|----------:|---------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 | 176.8 ms | 11.49 ms |  33.70 ms | 167.5 ms |
| TUnit     | .NET 8.0 | .NET 8.0 | 470.3 ms |  8.29 ms |  19.85 ms | 464.9 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 694.4 ms |  4.17 ms |   3.90 ms | 693.7 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 803.3 ms | 47.01 ms | 136.39 ms | 742.2 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 663.2 ms | 13.17 ms |  27.49 ms | 656.9 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 | 117.5 ms |  0.25 ms |   0.21 ms | 117.4 ms |
| TUnit     | .NET 9.0 | .NET 9.0 | 461.0 ms |  8.99 ms |  11.37 ms | 457.2 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 701.1 ms | 12.89 ms |  11.42 ms | 697.3 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 683.3 ms |  8.77 ms |   7.33 ms | 683.5 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 626.4 ms | 12.22 ms |  11.43 ms | 625.3 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method    | Job      | Runtime  | Mean        | Error     | StdDev    |
|---------- |--------- |--------- |------------:|----------:|----------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 |    87.16 ms |  1.720 ms |  4.219 ms |
| TUnit     | .NET 8.0 | .NET 8.0 |   849.04 ms | 16.466 ms | 26.589 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 1,334.09 ms | 15.615 ms | 13.040 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 1,328.56 ms | 18.563 ms | 16.455 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 1,188.48 ms | 16.984 ms | 15.887 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 |    64.06 ms |  0.909 ms |  0.806 ms |
| TUnit     | .NET 9.0 | .NET 9.0 |   821.84 ms | 16.381 ms | 27.369 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 1,319.33 ms | 18.861 ms | 17.643 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 1,287.32 ms |  7.951 ms |  7.048 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 1,165.25 ms | 12.304 ms | 11.509 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method    | Job      | Runtime  | Mean       | Error    | StdDev   | Median     |
|---------- |--------- |--------- |-----------:|---------:|---------:|-----------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 |   124.3 ms |  2.37 ms |  1.85 ms |   124.9 ms |
| TUnit     | .NET 8.0 | .NET 8.0 |   850.2 ms | 16.94 ms | 24.29 ms |   851.1 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 1,315.3 ms | 10.02 ms |  9.38 ms | 1,317.7 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 1,304.1 ms | 10.61 ms |  9.92 ms | 1,303.9 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 1,186.5 ms | 17.64 ms | 16.50 ms | 1,184.7 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 |   125.6 ms |  1.37 ms |  1.21 ms |   124.9 ms |
| TUnit     | .NET 9.0 | .NET 9.0 |   867.0 ms | 17.28 ms | 25.33 ms |   851.3 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 1,339.3 ms | 13.95 ms | 12.37 ms | 1,340.3 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 1,317.5 ms |  9.31 ms |  8.70 ms | 1,319.0 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 1,194.8 ms | 14.55 ms | 13.61 ms | 1,198.2 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.7 (23H124) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), Arm64 RyuJIT AdvSIMD
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), Arm64 RyuJIT AdvSIMD


```
| Method    | Job      | Runtime  | Mean        | Error     | StdDev    |
|---------- |--------- |--------- |------------:|----------:|----------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 |    287.5 ms |  15.18 ms |  44.53 ms |
| TUnit     | .NET 8.0 | .NET 8.0 |    630.7 ms |  20.78 ms |  60.27 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 14,472.7 ms | 280.12 ms | 676.53 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 14,554.4 ms | 290.54 ms | 538.54 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 14,708.5 ms | 280.04 ms | 572.05 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 |    291.2 ms |  15.71 ms |  46.31 ms |
| TUnit     | .NET 9.0 | .NET 9.0 |    619.3 ms |  20.14 ms |  59.37 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 14,166.0 ms | 275.39 ms | 394.95 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 14,356.9 ms | 286.78 ms | 494.68 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 14,423.7 ms | 283.26 ms | 532.04 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method    | Job      | Runtime  | Mean       | Error    | StdDev   |
|---------- |--------- |--------- |-----------:|---------:|---------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 |   134.9 ms |  2.69 ms |  6.07 ms |
| TUnit     | .NET 8.0 | .NET 8.0 |   918.3 ms | 18.17 ms | 30.85 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 6,556.3 ms | 21.49 ms | 19.05 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 6,530.6 ms | 26.33 ms | 24.63 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 6,471.2 ms | 10.65 ms |  9.97 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 |   124.2 ms |  2.48 ms |  6.07 ms |
| TUnit     | .NET 9.0 | .NET 9.0 |   892.6 ms | 17.64 ms | 29.95 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 6,499.6 ms | 23.38 ms | 21.87 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 6,467.8 ms | 18.79 ms | 16.66 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 6,442.9 ms | 28.44 ms | 25.21 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2700) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2


```
| Method    | Job      | Runtime  | Mean       | Error    | StdDev   |
|---------- |--------- |--------- |-----------:|---------:|---------:|
| TUnit_AOT | .NET 8.0 | .NET 8.0 |   186.4 ms |  3.43 ms |  2.87 ms |
| TUnit     | .NET 8.0 | .NET 8.0 |   924.8 ms | 18.33 ms | 23.83 ms |
| NUnit     | .NET 8.0 | .NET 8.0 | 7,565.0 ms | 17.16 ms | 15.21 ms |
| xUnit     | .NET 8.0 | .NET 8.0 | 7,549.5 ms | 19.40 ms | 17.20 ms |
| MSTest    | .NET 8.0 | .NET 8.0 | 7,511.5 ms | 28.97 ms | 27.10 ms |
| TUnit_AOT | .NET 9.0 | .NET 9.0 |   178.2 ms |  3.54 ms |  8.33 ms |
| TUnit     | .NET 9.0 | .NET 9.0 |   968.3 ms | 19.14 ms | 26.83 ms |
| NUnit     | .NET 9.0 | .NET 9.0 | 7,575.6 ms | 23.91 ms | 21.20 ms |
| xUnit     | .NET 9.0 | .NET 9.0 | 7,548.2 ms | 21.35 ms | 19.97 ms |
| MSTest    | .NET 9.0 | .NET 9.0 | 7,485.9 ms | 29.34 ms | 26.01 ms |



