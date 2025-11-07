# Running your tests

As TUnit is built on-top of the newer Microsoft.Testing.Platform, and combined with the fact that TUnit tests are source generated, running your tests is available in a variety of ways.

:::info

Please note that for the coverage and trx report, you need to install [additional extensions](../extensions/extensions.md)

:::

## [dotnet run](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)

For a simple execution of a project, `dotnet run` is the preferred method, allowing easier passing in of command line flags.

```powershell
cd 'C:/Your/Test/Directory'
dotnet run -c Release
# or with flags
dotnet run -c Release --report-trx --coverage
```

## [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

`dotnet test` requires any command line flags to be specified as application arguments, meaning after a `--` - Otherwise you'll get an error about unknown switches.

```powershell
cd 'C:/Your/Test/Directory'
dotnet test -c Release
# or with flags
dotnet test -c Release -- --report-trx --coverage
```

## [dotnet exec](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet)

If your test project has already been built, you can use `dotnet exec` or just `dotnet` with the `.dll` path

```powershell
cd 'C:/Your/Test/Directory/bin/Release/net8.0'
dotnet exec YourTestProject.dll
# or with flags
dotnet exec YourTestProject.dll --report-trx --coverage
```

or

```powershell
cd 'C:/Your/Test/Directory/bin/Release/net8.0'
dotnet YourTestProject.dll
# or with flags
dotnet YourTestProject.dll --report-trx --coverage
```

## Published Test Project

When you publish your test project, you'll be given an executable.
On windows this'll be a `.exe` and on Linux/MacOS there'll be no extension.

This can be invoked directly and passed any flags.

```powershell
cd 'C:/Your/Test/Directory/bin/Release/net8.0/win-x64/publish'
./YourTestProject.exe
# or with flags
./YourTestProject.exe --report-trx --coverage
```

# IDE Support

## Visual Studio
Visual Studio is supported. The "Use testing platform server mode" option must be selected in Tools > Manage Preview Features.

![Visual Studio Settings](../../static/img/visual-studio.png)

## Rider
Rider is supported.

The Enable Testing Platform support option must be selected in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform.

![Rider Settings](../../static/img/rider.png)

## VS Code
Visual Studio Code is supported.

- Install the extension Name: [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- Go to the C# Dev Kit extension's settings
- Enable Dotnet > Test Window > Use Testing Platform Protocol

![Visual Studio Code Settings](../../static/img/visual-studio-code.png)

## What's Next?

You've successfully learned the basics of TUnit! You can now:
- Write tests with the `[Test]` attribute
- Run them via command line or your IDE
- See your test results

To continue your journey with TUnit, explore these topics:

**Core Testing Concepts:**
- **[Assertions](../assertions/getting-started.md)** - Learn TUnit's fluent assertion syntax
- **[Test Lifecycle](../test-lifecycle/setup.md)** - Set up and tear down test state with hooks
- **[Data-Driven Testing](../test-authoring/arguments.md)** - Run tests with multiple input values

**Common Tasks:**
- **[Mocking](../test-authoring/mocking.md)** - Use mocks and fakes in your tests
- **[Best Practices](../guides/best-practices.md)** - Write maintainable, reliable tests
- **[Cookbook](../guides/cookbook.md)** - Common testing patterns and recipes

**Advanced Features:**
- **[Parallelism](../parallelism/not-in-parallel.md)** - Control how tests run in parallel
- **[CI/CD Integration](../execution/ci-cd-reporting.md)** - Integrate TUnit into your pipeline

Need help? Check the [Troubleshooting & FAQ](../troubleshooting.md) guide.

