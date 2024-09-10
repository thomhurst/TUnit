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
| Method           | Mean        | Error     | StdDev    | Median      |
|----------------- |------------:|----------:|----------:|------------:|
| TUnit_AOT        |    81.67 ms |  0.461 ms |  0.409 ms |    81.54 ms |
| TUnit_SingleFile | 1,106.13 ms | 21.857 ms | 50.657 ms | 1,099.29 ms |
| TUnit            |   421.69 ms |  6.459 ms |  5.725 ms |   422.10 ms |
| NUnit            |   692.26 ms |  8.743 ms |  7.301 ms |   693.18 ms |
| xUnit            |   687.01 ms | 13.016 ms | 26.880 ms |   676.55 ms |
| MSTest           |   636.61 ms | 12.546 ms | 18.778 ms |   631.85 ms |



#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    33.44 ms |  0.693 ms |  2.020 ms |
| TUnit_SingleFile |   435.83 ms |  4.463 ms |  3.956 ms |
| TUnit            |   826.54 ms | 16.493 ms | 39.517 ms |
| NUnit            | 1,401.28 ms | 19.298 ms | 18.051 ms |
| xUnit            | 1,386.21 ms | 19.169 ms | 16.007 ms |
| MSTest           | 1,251.04 ms | 11.875 ms | 11.108 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    79.33 ms |  1.510 ms |  1.339 ms |
| TUnit_SingleFile |   407.13 ms |  4.578 ms |  3.823 ms |
| TUnit            |   759.65 ms | 14.855 ms | 20.333 ms |
| NUnit            | 1,285.24 ms | 10.172 ms |  9.017 ms |
| xUnit            | 1,273.58 ms |  6.535 ms |  6.113 ms |
| MSTest           | 1,146.47 ms |  8.916 ms |  7.904 ms |


### Scenario: A test that takes 50ms to execute, repeated 100 times (including spawning a new process and initialising the test framework)

#### ubuntu-latest

```

BenchmarkDotNet v0.14.0, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean        | Error     | StdDev    | Median      |
|----------------- |------------:|----------:|----------:|------------:|
| TUnit_AOT        |    94.71 ms |  1.891 ms |  5.015 ms |    96.38 ms |
| TUnit_SingleFile |   494.41 ms |  8.037 ms |  7.518 ms |   491.90 ms |
| TUnit            |   883.23 ms | 17.502 ms | 38.417 ms |   876.18 ms |
| NUnit            | 6,571.52 ms | 19.864 ms | 18.580 ms | 6,573.48 ms |
| xUnit            | 6,583.63 ms | 37.632 ms | 35.201 ms | 6,577.24 ms |
| MSTest           | 6,541.67 ms | 23.173 ms | 20.543 ms | 6,548.66 ms |



#### windows-latest

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.2655) (Hyper-V)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method           | Mean       | Error     | StdDev    | Median     |
|----------------- |-----------:|----------:|----------:|-----------:|
| TUnit_AOT        |   152.9 ms |   3.04 ms |   5.07 ms |   155.8 ms |
| TUnit_SingleFile |   504.1 ms |   9.98 ms |  12.98 ms |   499.4 ms |
| TUnit            |   862.4 ms |  16.60 ms |  17.04 ms |   858.5 ms |
| NUnit            | 8,753.5 ms | 172.20 ms | 292.42 ms | 8,842.9 ms |
| xUnit            | 8,745.1 ms | 171.88 ms | 287.17 ms | 8,832.8 ms |
| MSTest           | 8,686.1 ms | 170.69 ms | 316.39 ms | 8,780.0 ms |



#### macos-latest

```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.6.1 (23G93) [Darwin 23.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.8 (8.0.824.36612), Arm64 RyuJIT AdvSIMD


```
| Method           | Mean        | Error     | StdDev    |
|----------------- |------------:|----------:|----------:|
| TUnit_AOT        |    244.0 ms |  11.95 ms |  35.05 ms |
| TUnit_SingleFile |  1,356.0 ms |  31.76 ms |  90.09 ms |
| TUnit            |    635.4 ms |  31.38 ms |  92.03 ms |
| NUnit            | 14,261.5 ms | 278.13 ms | 479.76 ms |
| xUnit            | 14,465.3 ms | 286.99 ms | 573.15 ms |
| MSTest           | 14,402.5 ms | 284.96 ms | 594.82 ms |


