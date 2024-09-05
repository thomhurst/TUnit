# TUnit

A modern, flexible and fast testing framework for .NET

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

### Scenario: A single test that completes instantly

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method | Mean       | Error    | StdDev   |
|------- |-----------:|---------:|---------:|
| TUnit  |   847.0 ms | 16.79 ms | 36.14 ms |
| NUnit  | 1,417.9 ms | 26.49 ms | 26.02 ms |
| xUnit  | 1,399.0 ms | 27.24 ms | 26.76 ms |
| MSTest | 1,264.6 ms | 17.92 ms | 15.88 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method | Mean       | Error    | StdDev   | Median     |
|------- |-----------:|---------:|---------:|-----------:|
| TUnit  |   757.3 ms | 14.99 ms | 21.02 ms |   744.6 ms |
| NUnit  | 1,265.3 ms |  6.01 ms |  5.33 ms | 1,266.1 ms |
| xUnit  | 1,267.1 ms | 11.11 ms |  9.85 ms | 1,267.4 ms |
| MSTest | 1,139.2 ms |  6.90 ms |  6.11 ms | 1,140.2 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method | Mean     | Error    | StdDev   | Median   |
|------- |---------:|---------:|---------:|---------:|
| TUnit  | 539.7 ms | 12.24 ms | 36.10 ms | 536.9 ms |
| NUnit  | 882.7 ms | 21.54 ms | 63.19 ms | 886.6 ms |
| xUnit  | 820.8 ms | 16.82 ms | 49.32 ms | 802.2 ms |
| MSTest | 704.6 ms | 14.08 ms | 15.06 ms | 701.4 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method | Mean       | Error    | StdDev   |
|------- |-----------:|---------:|---------:|
| TUnit  |   879.0 ms | 17.24 ms | 31.96 ms |
| NUnit  | 6,547.5 ms | 25.83 ms | 24.16 ms |
| xUnit  | 6,531.4 ms | 24.55 ms | 21.77 ms |
| MSTest | 6,491.0 ms | 25.45 ms | 23.81 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method | Mean       | Error     | StdDev    | Median     |
|------- |-----------:|----------:|----------:|-----------:|
| TUnit  |   839.3 ms |  16.42 ms |  21.92 ms |   853.9 ms |
| NUnit  | 8,555.9 ms | 170.40 ms | 472.19 ms | 8,777.4 ms |
| xUnit  | 8,763.8 ms |  28.09 ms |  26.28 ms | 8,769.1 ms |
| MSTest | 8,579.3 ms | 171.48 ms | 369.14 ms | 8,725.3 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method | Mean        | Error     | StdDev    |
|------- |------------:|----------:|----------:|
| TUnit  |    686.1 ms |  26.17 ms |  77.17 ms |
| NUnit  | 14,161.0 ms | 278.57 ms | 593.66 ms |
| xUnit  | 14,500.9 ms | 289.13 ms | 475.05 ms |
| MSTest | 14,166.0 ms | 277.50 ms | 370.45 ms |



