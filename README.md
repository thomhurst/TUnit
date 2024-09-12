# TUnit

A modern, flexible and fast testing framework for .NET 8 and up. With Native AOT and Single File application support included!

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

## IDE

TUnit is built on top of the newer Microsoft.Testing.Platform, as opposed to the older VSTest platform. As of September 2024, IDEs do not fully support this testing platform yet.

Visual Studio 17.10 onwards can run the new tests by enabling the new testing platform server mode, within Visual Studio preview/experimental features. You will have to opt in to this manually.

For Rider, it is not yet supported. I believe they are working on it so we just have to wait for now.

`dotnet` CLI - Fully supported. Tests should be runnable with `dotnet test` or `dotnet run`, `dotnet exec` or executing an executable directly. See the docs for more information!

## Features

- Native AOT + Single File application support
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

### Scenario: A single test that completes instantly (including spawning a new process and initialising the test framework)

#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method    | Mean      | Error     | StdDev    |
|---------- |----------:|----------:|----------:|
| TUnit_AOT |  84.02 ms |  1.668 ms |  3.050 ms |
| TUnit     | 424.94 ms |  8.067 ms |  8.967 ms |
| NUnit     | 692.02 ms | 10.960 ms | 10.252 ms |
| xUnit     | 676.84 ms |  9.650 ms |  8.555 ms |
| MSTest    | 617.53 ms | 11.766 ms | 10.431 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    41.39 ms |  1.341 ms |  3.933 ms |
| TUnit     |   821.96 ms | 16.356 ms | 34.500 ms |
| NUnit     | 1,384.19 ms | 26.084 ms | 27.909 ms |
| xUnit     | 1,362.20 ms | 16.903 ms | 14.984 ms |
| MSTest    | 1,217.93 ms | 16.207 ms | 13.533 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    82.41 ms |  1.619 ms |  3.001 ms |
| TUnit     |   768.31 ms | 15.215 ms | 19.784 ms |
| NUnit     | 1,323.33 ms | 24.835 ms | 20.738 ms |
| xUnit     | 1,302.99 ms | 16.359 ms | 14.502 ms |
| MSTest    | 1,175.98 ms |  7.434 ms |  6.954 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean        | Error     | StdDev    |
|---------- |------------:|----------:|----------:|
| TUnit_AOT |    89.82 ms |  1.791 ms |  4.749 ms |
| TUnit     |   883.21 ms | 17.242 ms | 31.528 ms |
| NUnit     | 6,604.23 ms | 23.219 ms | 20.583 ms |
| xUnit     | 6,568.17 ms | 26.496 ms | 24.784 ms |
| MSTest    | 6,510.06 ms | 18.351 ms | 17.165 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method    | Mean       | Error    | StdDev   |
|---------- |-----------:|---------:|---------:|
| TUnit_AOT |   150.1 ms |  2.93 ms |  5.06 ms |
| TUnit     |   886.4 ms | 17.72 ms | 25.41 ms |
| NUnit     | 7,584.9 ms | 15.45 ms | 14.45 ms |
| xUnit     | 7,568.1 ms | 14.08 ms | 12.48 ms |
| MSTest    | 7,526.4 ms | 14.57 ms | 13.63 ms |



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
| TUnit_AOT |    251.2 ms |  12.55 ms |  37.01 ms |
| TUnit     |    568.7 ms |  21.79 ms |  63.89 ms |
| NUnit     | 14,171.5 ms | 281.25 ms | 605.41 ms |
| xUnit     | 14,574.0 ms | 289.84 ms | 654.22 ms |
| MSTest    | 14,529.1 ms | 288.91 ms | 520.96 ms |


