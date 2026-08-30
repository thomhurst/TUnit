# Troubleshooting & FAQ

## FAQ[​](#faq "Direct link to FAQ")

### Why do I have to await assertions?[​](#why-do-i-have-to-await-assertions "Direct link to Why do I have to await assertions?")

Assertions don't execute until awaited — forgetting `await` means the test passes silently. See [Awaiting Assertions](/docs/assertions/awaiting.md) for details.

TUnit includes code fixers to help with migration from [xUnit](/docs/migration/xunit.md#automated-migration-with-code-fixers), [NUnit](/docs/migration/nunit.md#automated-migration-with-code-fixers), and [MSTest](/docs/migration/mstest.md#automated-migration-with-code-fixers).

### Does TUnit work with Coverlet?[​](#does-tunit-work-with-coverlet "Direct link to Does TUnit work with Coverlet?")

No. Coverlet only works with the VSTest platform. TUnit uses `Microsoft.Testing.Platform`, so you need `Microsoft.Testing.Extensions.CodeCoverage` instead — this is already included in the `TUnit` meta package.

### `dotnet test` vs `dotnet run`?[​](#dotnet-test-vs-dotnet-run "Direct link to dotnet-test-vs-dotnet-run")

TUnit uses `Microsoft.Testing.Platform`, not VSTest, but both commands work. Prefer `dotnet test` — it runs against every targeted TFM automatically, whereas `dotnet run` only runs a single TFM. On the .NET 10+ SDK, platform flags can be passed directly (no `--` separator needed).

```
# Run all tests (across every targeted TFM)

dotnet test



# Pass flags directly

dotnet test --treenode-filter "/*/*/MyTestClass/*"
```

## Tests Not Discovered[​](#tests-not-discovered "Direct link to Tests Not Discovered")

If no tests appear in the test explorer or `dotnet test` reports 0 tests, check these in order:

**Missing TUnit package:**

```
<PackageReference Include="TUnit" Version="*" />
```

**Microsoft.NET.Test.Sdk conflict** — remove it, it conflicts with TUnit's platform:

```
<!-- Remove this -->

<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

**Missing `[Test]` attribute:**

```
[Test]

public async Task MyTest() { }
```

**Non-public or static test methods** — test methods must be public instance methods.

**Wrong OutputType** — if you see `hostfxr.dll could not be found`, check your `.csproj`:

```
<OutputType>Exe</OutputType>
```

## IDE Setup[​](#ide-setup "Direct link to IDE Setup")

TUnit requires `Microsoft.Testing.Platform` support to be enabled in your IDE.

As a first step, try a clean rebuild of your solution.

### Visual Studio[​](#visual-studio "Direct link to Visual Studio")

1. Tools > Options > Preview Features > enable "Use testing platform server mode".
2. Restart IDE.

If tests are still not discovered, try deleting the `.vs` folder in your solution directory.

### Rider[​](#rider "Direct link to Rider")

1. Settings > Build, Execution, Deployment > Unit Testing > Testing Platform > turn on the "Enable Testing Platform support".
2. Restart IDE.

If tests still don't appear, it may be a conflict with VSTest. You can fix it in two ways:

1. **Recommended:** Remove all project name masks in Settings > Build, Execution, Deployment > Unit Testing > VSTest > Projects with unit tests.
2. **Alternative:** Disable "Ignore projects discovered by other test frameworks" in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform.

If none of the above helps, try invalidating caches via File > Invalidate Caches and restart the IDE.

### VS Code[​](#vs-code "Direct link to VS Code")

1. Install C# Dev Kit, then reload.

## Test Filtering[​](#test-filtering "Direct link to Test Filtering")

TUnit uses tree-node filter syntax, not the VSTest filter syntax.

**Pattern:** `/Assembly/Namespace/Class/Method[Property=Value]`

```
# All tests in a class

dotnet test --treenode-filter "/*/*/MyTestClass/*"



# A specific test method

dotnet test --treenode-filter "/*/*/MyTestClass/MyTestMethod"



# By category

dotnet test --treenode-filter "/*/*/*/*[Category=Integration]"



# Exclude a category

dotnet test --treenode-filter "/*/*/*/*[Category!=Performance]"



# Multiple filters (OR)

dotnet test --treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"



# Combine filters (AND)

dotnet test --treenode-filter "/*/*/*/*[Category=Integration][Priority=High]"
```

## AOT Compilation Errors[​](#aot-compilation-errors "Direct link to AOT Compilation Errors")

If you see trim warnings or "source generator did not generate" errors, make sure you're using AOT-compatible data sources:

Replace reflection-based `MethodDataSource(typeof(DataClass), "GetData")` usage with the generic form:

```
public sealed class DataClass

{

    public static IEnumerable<int> GetData() => [1, 2, 3];

}



public class DataSourceTests

{

    [Test]

    [MethodDataSource<DataClass>(nameof(DataClass.GetData))]

    public void ReceivesData(int value)

    {

        Console.WriteLine(value);

    }

}
```

## InstanceMethodDataSource Returns No Tests[​](#instancemethoddatasource-returns-no-tests "Direct link to InstanceMethodDataSource Returns No Tests")

If you're using `InstanceMethodDataSource` with a `ClassDataSource` fixture that implements `IAsyncInitializer`, tests won't appear during discovery. The fixture hasn't been initialised yet at discovery time, so the data source returns nothing.

The fix is to return predefined identifiers that don't depend on initialisation:

```
public class Fixture : IAsyncInitializer

{

    private static readonly string[] TestCaseIds = ["Case1", "Case2", "Case3"];



    public async Task InitializeAsync()

    {

        await StartDockerContainerAsync();

    }



    public IEnumerable<string> GetTestCaseIds() => TestCaseIds;



    private static Task StartDockerContainerAsync() => Task.CompletedTask;

}
```

## Hooks Not Running[​](#hooks-not-running "Direct link to Hooks Not Running")

Class-level and assembly-level hooks must be static. An instance declaration such as `public void ClassSetup()` is invalid:

```
public class HookExamples

{

    [Before(Class)]

    public static void ClassSetup()

    {

    }

}
```

Test-level hooks (`[Before(Test)]` / `[After(Test)]`) can be instance methods.

## Code Coverage[​](#code-coverage "Direct link to Code Coverage")

The `TUnit` meta package includes `Microsoft.Testing.Extensions.CodeCoverage` automatically. If you're using `TUnit.Engine` directly, add it manually:

```
<PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" Version="*" />
```

**Basic usage:**

```
dotnet run --configuration Release --coverage

dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

If you have Coverlet installed from a previous framework, remove it — `coverlet.collector` and `coverlet.msbuild` are not compatible with TUnit.

## Getting Help[​](#getting-help "Direct link to Getting Help")

1. Search [GitHub Issues](https://github.com/thomhurst/TUnit/issues) for similar problems
2. Run with `--diagnostic` for detailed logs
3. If it's a bug, open an issue with your TUnit version, .NET version, and a minimal reproduction
