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
- Parallel by default, with mechanisms to switch it off for certain tests
- Test ordering (if running not in parallel)
- Tests can depend on other tests to form chains
- Easy to read assertions
- Injectable test data functionality
- Hooks before and after: Assembly, Class, Test
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
    [BeforeAllTestsInClass]
    public static async Task ClearDatabase(ClassHookContext context) { ... }

    [AfterAllTestsInClass]
    public static async Task AssertDatabaseIsAsExpected(ClassHookContext context) { ... }

    [BeforeEachTest]
    public async Task CreatePlaywrightBrowser(TestContext context) { ... }

    [AfterEachTest]
    public async Task DisposePlaywrightBrowser(TestContext context) { ... }

    [Retry(3)]
    [Test, DisplayName("Register an account")]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task Register(string username, string password) { ... }

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

    [Test, Explicit, WindowsOnlyTest, RetryHttpServiceUnavailable]
    [Property("Some Key", "Some Value")]
    public async Task Ping() { ... }

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
```

## Motivations

TUnit is inspired by NUnit and xUnit - two of the most popular testing frameworks for .NET.

It aims to build upon the useful features of both while trying to address any pain points that they may have. You may have experienced these, or you may have not even known and experienced flakiness or bugs due to it.

NUnit by default creates 1 test class for all the tests within it, and runs them all against the same instance. Therefore if a test stores or accesses any state, it could be a hangover from another test, meaning leaky test states and potentially dodgy data.

xUnit doesn't offer a way out-of-the-box to retrieve the test state within a hook. For example, running UI tests, in a tear down method, you might want to take a screenshot but only if the test failed. The hook is written generically to work with every test, but we don't have anything available to us to query for the test status.

xUnit performs tear downs through interfaces such as IDisposable. TUnit also allows this, but a problem occurs if you want to use test class inheritance. Say you have a class that wants to take a screenshot, and then a base class that disposes the browser. You have to declare another `Dispose()` method, and hide the visibility of the base one with the `new` keyword, which is generally frowned up. You then also have to remember to call `base.Dispose()` - and if you don't, you may have bugs and/or unreleased resources. And then to top it off, you have to manage exceptions yourself to ensure they still run. In TUnit, all cleanup methods will run, even if previous code has encountered exceptions.

xUnit assertions are fairly basic and have the problem of it being unclear which argument goes in which position:

```csharp
var one = 2;
Assert.Equal(1, one)
Assert.Equal(one, 1)
```

NUnit assertions largely influenced the way that TUnit assertions work. However, NUnit assertions do not have compile time checks. I could check if a string is negative (`NUnitAssert.That("String", Is.Negative);`) or if a boolean throws an exception (`NUnitAssert.That(true, Throws.ArgumentException);`). These assertions don't make sense. There are analyzers to help catch these - But they will compile if these analyzers aren't run.
TUnit assertions are built with the type system in mind. Specific assertions are built via extensions to the relevant types, and not in a generic sense that could apply to anything. That means when you're using intellisense to see what methods you have available, you should only see assertions that are relevant for your type. This makes it harder to make mistakes, and decreases your feedback loop time.

## Extras

TUnit offers a few extra bits that NUnit and xUnit do not (that I'm aware of):

- Tests are source generated - And not relied upon using reflection to discover them
- Hooks are available at the assembly, class and test level, each with respective objects available to be referenced in your hook methods. An `AssemblyHookContext` object will have details on the current assembly and a collection of all the tests its discovered. If you're in a tear down, each test object will have details of its result. A `ClassHookContext` is the same, but with details of the class and its tests instead. And a `TestContext` will just be the details for a single test.
- Dependent tests - Tests can depend on another and delay their execution until their dependencies have finished - Without turning off parallelism or having to manually work out the best way to configure this.
