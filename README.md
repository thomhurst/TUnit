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
| Method       | Mean    | Error    | StdDev   | Median  |
|------------- |--------:|---------:|---------:|--------:|
| Build_TUnit  | 1.409 s | 0.1080 s | 0.3150 s | 1.334 s |
| Build_NUnit  | 1.032 s | 0.0402 s | 0.1167 s | 1.011 s |
| Build_xUnit  | 1.142 s | 0.0964 s | 0.2828 s | 1.047 s |
| Build_MSTest | 1.136 s | 0.0600 s | 0.1769 s | 1.124 s |



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
| Build_TUnit  | 1.879 s | 0.0337 s | 0.0299 s |
| Build_NUnit  | 1.548 s | 0.0249 s | 0.0208 s |
| Build_xUnit  | 1.581 s | 0.0313 s | 0.0438 s |
| Build_MSTest | 1.683 s | 0.0228 s | 0.0213 s |



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
| Build_TUnit  | 1.895 s | 0.0372 s | 0.0521 s |
| Build_NUnit  | 1.616 s | 0.0275 s | 0.0243 s |
| Build_xUnit  | 1.617 s | 0.0315 s | 0.0323 s |
| Build_MSTest | 1.608 s | 0.0165 s | 0.0154 s |


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
| Method    | Mean     | Error    | StdDev    | Median   |
|---------- |---------:|---------:|----------:|---------:|
| TUnit_AOT | 171.5 ms | 11.64 ms |  34.31 ms | 161.0 ms |
| TUnit     | 667.3 ms | 47.87 ms | 140.39 ms | 650.6 ms |
| NUnit     | 916.5 ms | 44.47 ms | 130.42 ms | 896.9 ms |
| xUnit     | 807.0 ms | 23.60 ms |  66.17 ms | 796.3 ms |
| MSTest    | 714.2 ms | 33.86 ms |  97.68 ms | 675.6 ms |



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
| TUnit_AOT |    66.59 ms |  1.318 ms |  2.508 ms |
| TUnit     |   847.00 ms | 16.857 ms | 24.709 ms |
| NUnit     | 1,390.75 ms | 25.863 ms | 24.192 ms |
| xUnit     | 1,324.67 ms | 25.327 ms | 26.009 ms |
| MSTest    | 1,208.34 ms | 21.444 ms | 20.059 ms |



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
| TUnit_AOT |   123.7 ms |  2.43 ms |  3.24 ms |
| TUnit     |   895.4 ms | 17.44 ms | 19.39 ms |
| NUnit     | 1,357.5 ms | 10.57 ms |  9.37 ms |
| xUnit     | 1,329.3 ms | 20.57 ms | 19.24 ms |
| MSTest    | 1,196.9 ms | 13.11 ms | 11.62 ms |


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
| TUnit_AOT |    324.0 ms |  17.06 ms |  48.69 ms |
| TUnit     |    847.4 ms |  28.99 ms |  84.11 ms |
| NUnit     | 14,183.4 ms | 280.67 ms | 598.14 ms |
| xUnit     | 14,456.6 ms | 284.54 ms | 505.77 ms |
| MSTest    | 14,411.2 ms | 286.43 ms | 544.96 ms |



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
| TUnit_AOT |   136.7 ms |  2.70 ms |  5.21 ms |
| TUnit     |   911.3 ms | 17.97 ms | 26.90 ms |
| NUnit     | 6,513.9 ms | 23.15 ms | 21.66 ms |
| xUnit     | 6,473.7 ms | 21.35 ms | 19.97 ms |
| MSTest    | 6,454.5 ms | 17.52 ms | 15.53 ms |



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
| TUnit_AOT |   175.3 ms |  3.49 ms |  9.33 ms |
| TUnit     |   955.9 ms | 18.64 ms | 22.89 ms |
| NUnit     | 7,566.3 ms | 19.33 ms | 18.08 ms |
| xUnit     | 7,593.8 ms | 10.59 ms |  9.91 ms |
| MSTest    | 7,513.6 ms | 11.57 ms | 10.82 ms |



