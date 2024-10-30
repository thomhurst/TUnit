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

## Modern and Fast
TUnit leverages source generators to locate and register your tests as opposed to reflection. You'll have a slight bump in build time, but a speedier runtime.

TUnit also builds upon the newer Microsoft.Testing.Platform, whereas most other frameworks you'll have used will use VSTest. The new platform was reconstructed from the ground up to address pain points, be more extensible, and be faster.

## Hooks, Events and Lifecycles
One of the most powerful parts of TUnit is the information you have available to you because of the source generation and the events you can subscribe to.
Because tests are constructed at the point of discovery, and not at runtime, you know all your arguments, properties, etc. upfront.

You can then register to be notified about various events such as test registered (scheduled to run in this test session at some point in the future), test started, test finished, etc.

Say we injected an external object into our tests:
By knowing how many tests are registered, we could count them up, and then on a test end event, we could decrease the count. When hitting 0, we know our object isn't going to be used by any other tests, so we can dispose of it. We know when we can handle the lifecycle, and this prevents it from living till the end of the test session where it could be hanging on to precious resources.

## Built in Analyzers
TUnit tries to help you write your tests correctly with analyzers. If something isn't quite right, an analyzer should tell you what's wrong.

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

## Packages

### TUnit.Core
To be used when you want to define re-useable components, such as a test library, but it wouldn't be run as its own test suite.

### TUnit.Engine
For test suites. This contains the test execution logic and test adapter. Only install this on actual test projects you intend to run, not class libraries.

### TUnit.Assertions
This is independent from the framework and can be used wherever - Even in other test frameworks. It is just an assertion library used to assert data is as you expect. It uses an asychronous syntax which may be different to other assertion libraries you may have used.

### TUnit
This is a helper package to combine the above 3 packages. If you just want a standard test app where you can write, run and assert tests, just install this!

### TUnit.Playwright
This provides you base classes, similarly to Microsoft.Playwright.NUnit or Microsoft.Playwright.MSTest, to automatically create and dispose of Playwright objects in tests, to make it easier for you to write tests without worrying about lifecycles or disposing. The base classes are named the same as the other libraries: `PageTest`,  `ContextTest`, `BrowserTest`, and `PlaywrightTest`.

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
| Method       | Mean       | Error    | StdDev    | Median     |
|------------- |-----------:|---------:|----------:|-----------:|
| Build_TUnit  | 1,103.7 ms | 64.43 ms | 186.91 ms | 1,044.2 ms |
| Build_NUnit  |   857.8 ms | 17.02 ms |  48.85 ms |   848.6 ms |
| Build_xUnit  | 1,028.3 ms | 58.24 ms | 170.81 ms |   985.0 ms |
| Build_MSTest |   917.2 ms | 21.41 ms |  63.13 ms |   909.8 ms |



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
| Build_TUnit  | 1.819 s | 0.0360 s | 0.0442 s |
| Build_NUnit  | 1.552 s | 0.0267 s | 0.0250 s |
| Build_xUnit  | 1.568 s | 0.0170 s | 0.0142 s |
| Build_MSTest | 1.611 s | 0.0203 s | 0.0189 s |



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
| Build_TUnit  | 1.792 s | 0.0259 s | 0.0229 s |
| Build_NUnit  | 1.502 s | 0.0187 s | 0.0175 s |
| Build_xUnit  | 1.504 s | 0.0160 s | 0.0150 s |
| Build_MSTest | 1.560 s | 0.0169 s | 0.0158 s |


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
| Method    | Mean      | Error     | StdDev    | Median    |
|---------- |----------:|----------:|----------:|----------:|
| TUnit_AOT |  84.84 ms |  1.851 ms |  5.160 ms |  82.39 ms |
| TUnit     | 583.27 ms | 31.837 ms | 92.365 ms | 576.58 ms |
| NUnit     | 815.33 ms | 28.719 ms | 84.680 ms | 802.88 ms |
| xUnit     | 713.25 ms | 14.179 ms | 26.632 ms | 704.05 ms |
| MSTest    | 657.59 ms | 13.113 ms | 32.168 ms | 647.30 ms |



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
| TUnit_AOT |    31.86 ms |  0.754 ms |  2.211 ms |
| TUnit     |   827.45 ms | 16.199 ms | 25.221 ms |
| NUnit     | 1,315.31 ms | 19.117 ms | 17.882 ms |
| xUnit     | 1,290.41 ms | 12.637 ms | 10.552 ms |
| MSTest    | 1,172.50 ms | 17.973 ms | 16.812 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2762) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    77.77 ms |  0.678 ms |  0.566 ms |
| TUnit     |   879.80 ms | 17.444 ms | 25.017 ms |
| NUnit     | 1,392.98 ms | 26.998 ms | 32.139 ms |
| xUnit     | 1,359.93 ms | 26.806 ms | 25.074 ms |
| MSTest    | 1,212.27 ms | 13.728 ms | 12.169 ms |


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
| TUnit_AOT |    252.8 ms |  13.96 ms |  41.16 ms |
| TUnit     |    704.4 ms |  30.57 ms |  87.72 ms |
| NUnit     | 14,185.9 ms | 275.20 ms | 337.97 ms |
| xUnit     | 14,627.1 ms | 289.89 ms | 507.72 ms |
| MSTest    | 14,334.6 ms | 284.30 ms | 593.45 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.5 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean       | Error    | StdDev   | Median     |
|---------- |-----------:|---------:|---------:|-----------:|
| TUnit_AOT |   105.0 ms |  2.07 ms |  3.62 ms |   106.9 ms |
| TUnit     |   895.6 ms | 11.69 ms |  9.76 ms |   896.7 ms |
| NUnit     | 6,499.8 ms | 12.36 ms | 10.96 ms | 6,502.7 ms |
| xUnit     | 6,463.4 ms | 13.76 ms | 12.20 ms | 6,459.9 ms |
| MSTest    | 6,443.3 ms | 31.42 ms | 29.39 ms | 6,431.0 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2762) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]   : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method    | Mean       | Error    | StdDev   | Median     |
|---------- |-----------:|---------:|---------:|-----------:|
| TUnit_AOT |   130.2 ms |  2.57 ms |  5.08 ms |   128.1 ms |
| TUnit     |   949.9 ms | 18.52 ms | 24.72 ms |   938.4 ms |
| NUnit     | 7,619.3 ms | 28.45 ms | 23.76 ms | 7,625.5 ms |
| xUnit     | 7,591.4 ms | 18.95 ms | 17.73 ms | 7,596.3 ms |
| MSTest    | 7,497.0 ms | 12.39 ms | 10.99 ms | 7,492.7 ms |



