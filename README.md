# TUnit

T(est)Unit!

## Documentation

See here: <https://thomhurst.github.io/TUnit/>

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
- Ability to view metadata and results (if in a cleanup method) for a test from a `TestContext` object

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
    public static async Task ClearDatabase() { ... }

    [AfterAllTestsInClass]
    public static async Task AssertDatabaseIsAsExpected() { ... }

    [BeforeEachTest]
    public async Task CreatePlaywrightBrowser() { ... }

    [AfterEachTest]
    public async Task DisposePlaywrightBrowser() { ... }

    [Retry(3)]
    [Test, DisplayName("Register an account")]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task Register(string username, string password) { ... }

    [DataSourceDrivenTest, DependsOn(nameof(Register))]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task Login(string username, string password) { ... }

    [DataSourceDrivenTest, DependsOn(nameof(Login))]
    [EnumerableMethodData(nameof(GetAuthDetails))]
    public async Task DeleteAccount(string username, string password) { ... }

    [Test, NotInParallel(Order = 1)]
    public async Task DownloadFile1() { ... }

    [Test, NotInParallel(Order = 2)]
    public async Task DownloadFile2() { ... }

    [Repeat(10)]
    [DataDrivenTest]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task GoToPage(int page) { ... }

    public static IEnumerable<(string Username, string Password)> GetAuthDetails()
    {
        yield return ("user1", "password1");
        yield return ("user2", "password2");
        yield return ("user3", "password3");
    }
```

## Motivations

There are only three main testing frameworks in the .NET world - xUnit, NUnit and MSTest. More frameworks means more options, and more options motivates more features or improvements.

These testing frameworks are amazing, but I've had some issues with them. You might not have had any of these, but these are my experiences:

### xUnit

There is no way to tap into information about a test in a generic way. For example, I've had some Playwright tests run before, and I want them to save a screenshot or video ONLY when the test fails. If the test passes, I don't have anything to investigate, and it'll use up unnecessary storage, and it'll probably slow my test suite down if I had hundreds or thousands of tests all trying to save screenshots.

However, if I'm in a Dispose method which is called when the test ends, then there's no way for me to know if my test succeeded or failed. I'd have to do some really clunky workaround involving try catch and setting a boolean or exception to a class field and checking that. And to do that for every test was just not ideal.

#### Assertions

I have stumbled across assertions so many times where the arguments are the wrong way round. This can result in really confusing error messages.

```csharp
var one = 2;
Assert.Equal(1, one)
Assert.Equal(one, 1)
```

### NUnit

#### Assertions

I absolutely love the newer assertion syntax in NUnit. The `Assert.That(something, Is.Something)`. I think it's really clear to read, it's clear what is being asserted, and it's clear what you're trying to achieve.

However, there is a lack of type checking on assertions. (Yes, there are analyzer packages to help with this, but this still isn't strict type checking.)

`Assert.That("1", Throws.Exception);`

This assertion makes no sense, because we're passing in a string. This can never throw an exception because it isn't a delegate that can be executed. But it's still perfectly valid code that will compile.

As does this: `Assert.That(1, Does.Contain("Foo!"));`

An integer can not contain a string. Of course these will fail at runtime, but we could move these errors up to compile time for faster feedback. This is very useful for long pipelines or build times.

Some methods also just read a little bit weird: `Assert.That(() => Something(), Throws.Exception.Message.Contain(someMessage));`

"Throws Exception Message Contain someMessage" - It's not terrible, but it could read a little better.

With TUnit assertions, I wanted to make these impossible to compile. So type constraints are built into the assertions themselves. There should be no way for a non-delegate to be able to do a `Throws` assertion, or for an `int` assertion to check for `string` conditions.

So in TUnit, this will compile:

```csharp
await Assert.That(() => GetSomeValue()).Throws.Nothing;
```

This won't:

```csharp
await Assert.That(GetSomeValue()).Throws.Nothing;
```
