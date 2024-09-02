# TUnit

T(est)Unit!

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## Benchmark

Scenario: A test that takes 50ms to execute, repeated 100 times.

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean       | Error    | StdDev   |
|------- |-----------:|---------:|---------:|
| TUnit  |   893.3 ms | 773.7 ms | 42.41 ms |
| NUnit  | 6,527.8 ms | 290.0 ms | 15.89 ms |
| xUnit  | 6,528.1 ms | 494.9 ms | 27.13 ms |
| MSTest | 6,505.3 ms | 515.8 ms | 28.27 ms |

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean       | Error    | StdDev   |
|------- |-----------:|---------:|---------:|
| TUnit  |   806.1 ms | 437.2 ms | 23.96 ms |
| NUnit  | 7,469.9 ms | 267.5 ms | 14.66 ms |
| xUnit  | 7,430.0 ms | 118.3 ms |  6.49 ms |
| MSTest | 7,389.2 ms | 360.5 ms | 19.76 ms |

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean        | Error       | StdDev    |
|------- |------------:|------------:|----------:|
| TUnit  |    593.6 ms |  1,184.7 ms |  64.94 ms |
| NUnit  | 14,658.5 ms |  6,370.3 ms | 349.18 ms |
| xUnit  | 14,712.5 ms | 11,572.7 ms | 634.34 ms |
| MSTest | 14,784.0 ms |  4,218.8 ms | 231.25 ms |


## IDE

TUnit is built on top of newer Microsoft.Testing.Platform libraries, as opposed to older legacy VSTest libraries. As of July 2024, IDEs do not fully support this testing platform yet.

Visual Studio Preview versions can run the new tests by enabling the new testing platform server mode, within Visual Studio preview/experimental features. You will have to opt in to this manually.

For Rider, it is not yet supported. I believe they are working on it so we just have to wait for now.

`dotnet` CLI - Fully supported. Tests should be runnable with both `dotnet test` or `dotnet run`. `dotnet run` should give you a better experience and make it simpler to pass in test flags!

## Features

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