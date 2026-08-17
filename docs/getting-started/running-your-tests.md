# Running your tests

As TUnit is built on-top of the newer Microsoft.Testing.Platform, and combined with the fact that TUnit tests are source generated, running your tests is available in a variety of ways.

info

Coverage and TRX reporting are built in. See [Extensions](/docs/extending/built-in-extensions.md) for usage flags.

## [dotnet run](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)[​](#dotnet-run "Direct link to dotnet-run")

For a simple execution of a project, `dotnet run` is the preferred method, allowing easier passing in of command line flags.

```
cd 'C:/Your/Test/Directory'

dotnet run -c Release

# or with flags

dotnet run -c Release --report-trx --coverage
```

## [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)[​](#dotnet-test "Direct link to dotnet-test")

`dotnet test` requires any command line flags to be specified as application arguments, meaning after a `--` - Otherwise you'll get an error about unknown switches.

```
cd 'C:/Your/Test/Directory'

dotnet test -c Release

# or with flags

dotnet test -c Release -- --report-trx --coverage
```

## [dotnet exec](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet)[​](#dotnet-exec "Direct link to dotnet-exec")

If your test project has already been built, you can use `dotnet exec` or just `dotnet` with the `.dll` path

```
cd 'C:/Your/Test/Directory/bin/Release/net8.0'

dotnet exec YourTestProject.dll

# or with flags

dotnet exec YourTestProject.dll --report-trx --coverage
```

or

```
cd 'C:/Your/Test/Directory/bin/Release/net8.0'

dotnet YourTestProject.dll

# or with flags

dotnet YourTestProject.dll --report-trx --coverage
```

## Published Test Project[​](#published-test-project "Direct link to Published Test Project")

When you publish your test project, you'll be given an executable. On Windows this will be a `.exe` and on Linux/macOS there will be no extension.

This can be invoked directly and passed any flags.

```
cd 'C:/Your/Test/Directory/bin/Release/net8.0/win-x64/publish'

./YourTestProject.exe

# or with flags

./YourTestProject.exe --report-trx --coverage
```

## IDE Support[​](#ide-support "Direct link to IDE Support")

## Visual Studio[​](#visual-studio "Direct link to Visual Studio")

Visual Studio is supported. The "Use testing platform server mode" option must be selected in Tools > Manage Preview Features.

![Visual Studio Settings](/assets/images/visual-studio-9d0c07a059c8661637788830d1c06c83.png)

## Rider[​](#rider "Direct link to Rider")

Rider is supported.

The "Enable Testing Platform support" option must be selected in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform.

![Rider Settings](/assets/images/rider-048cfc49b8bb20d54074d7f50f257ec4.png)

## VS Code[​](#vs-code "Direct link to VS Code")

Visual Studio Code is supported.

* Install the extension Name: [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
* Go to the C# Dev Kit extension's settings
* Enable Dotnet > Test Window > Use Testing Platform Protocol

![Visual Studio Code Settings](/assets/images/visual-studio-code-31c2cf7aaa93d6b85b0859d74b303469.png)

## What's Next?[​](#whats-next "Direct link to What's Next?")

You've successfully learned the basics of TUnit! You can now:

* Write tests with the `[Test]` attribute
* Run them via command line or your IDE
* See your test results

To continue your journey with TUnit, explore these topics:

**Core Testing Concepts:**

* **[Assertions](/docs/assertions/getting-started.md)** - Learn TUnit's fluent assertion syntax
* **[Test Lifecycle](/docs/writing-tests/lifecycle.md)** - Understand the test execution lifecycle
* **[Data-Driven Testing](/docs/writing-tests/arguments.md)** - Run tests with multiple input values

**Common Tasks:**

* **[Mocking](/docs/writing-tests/mocking/.md)** - Use mocks and fakes in your tests
* **[Tips & Pitfalls](/docs/guides/best-practices.md)** - TUnit-specific tips to avoid common mistakes

**Advanced Features:**

* **[Parallelism](/docs/execution/parallelism.md)** - Control how tests run in parallel
* **[CI/CD Integration](/docs/execution/ci-cd-reporting.md)** - Integrate TUnit into your pipeline

Need help? Check the [Troubleshooting & FAQ](/docs/troubleshooting.md) guide.
