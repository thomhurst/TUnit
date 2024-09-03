# TUnit

T(est)Unit!

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

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

## Benchmark

Scenario: A test that takes 50ms to execute, repeated 100 times.

### Scenario: A single test that completes instantly

macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean     | Error    | StdDev   |
|------- |---------:|---------:|---------:|
| TUnit  | 482.7 ms | 311.9 ms | 17.10 ms |
| NUnit  | 702.1 ms | 138.6 ms |  7.60 ms |
| xUnit  | 672.8 ms | 103.0 ms |  5.65 ms |
| MSTest | 619.9 ms | 250.3 ms | 13.72 ms |



windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean       | Error     | StdDev   |
|------- |-----------:|----------:|---------:|
| TUnit  |   781.8 ms | 416.53 ms | 22.83 ms |
| NUnit  | 1,285.2 ms | 322.75 ms | 17.69 ms |
| xUnit  | 1,251.8 ms |  92.85 ms |  5.09 ms |
| MSTest | 1,127.6 ms |  71.40 ms |  3.91 ms |



ubuntu-latest

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
| TUnit  |   807.5 ms | 715.3 ms | 39.21 ms |
| NUnit  | 1,375.3 ms | 725.8 ms | 39.78 ms |
| xUnit  | 1,378.5 ms | 449.1 ms | 24.61 ms |
| MSTest | 1,218.8 ms | 112.1 ms |  6.14 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times

windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]   : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Mean       | Error      | StdDev   |
|------- |-----------:|-----------:|---------:|
| TUnit  |   868.3 ms |   443.0 ms | 24.28 ms |
| NUnit  | 7,563.1 ms | 1,280.2 ms | 70.17 ms |
| xUnit  | 8,815.6 ms |   194.8 ms | 10.68 ms |
| MSTest | 8,777.8 ms |   326.7 ms | 17.91 ms |



ubuntu-latest

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
| TUnit  |   867.5 ms | 433.1 ms | 23.74 ms |
| NUnit  | 6,538.2 ms | 198.6 ms | 10.88 ms |
| xUnit  | 6,538.9 ms | 355.1 ms | 19.47 ms |
| MSTest | 6,489.1 ms | 430.7 ms | 23.61 ms |



macos-latest

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
| TUnit  |    866.8 ms |  1,656.0 ms |  90.77 ms |
| NUnit  | 14,419.1 ms |  1,405.4 ms |  77.03 ms |
| xUnit  | 14,347.4 ms | 10,275.8 ms | 563.25 ms |
| MSTest | 14,490.9 ms | 10,899.4 ms | 597.43 ms |


