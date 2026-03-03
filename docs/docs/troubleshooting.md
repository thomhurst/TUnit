# Troubleshooting & FAQ

## FAQ

### Why do I have to await assertions?

Assertions don't execute until awaited — forgetting `await` means the test passes silently. See [Awaiting Assertions](assertions/awaiting.md) for details.

TUnit includes code fixers to help with migration from [xUnit](migration/xunit.md#automated-migration-with-code-fixers), [NUnit](migration/nunit.md#automated-migration-with-code-fixers), and [MSTest](migration/mstest.md#automated-migration-with-code-fixers).

### Does TUnit work with Coverlet?

No. Coverlet only works with the VSTest platform. TUnit uses `Microsoft.Testing.Platform`, so you need `Microsoft.Testing.Extensions.CodeCoverage` instead — this is already included in the `TUnit` meta package.

### Why `dotnet run` instead of `dotnet test`?

TUnit uses `Microsoft.Testing.Platform`, not VSTest. `dotnet run` launches the test app directly. `dotnet test` can work but `dotnet run` gives you the best experience and the full set of CLI flags.

```bash
# Run all tests
dotnet run

# Pass flags after --
dotnet run -- --treenode-filter "/*/*/MyTestClass/*"
```

## Tests Not Discovered

If no tests appear in the test explorer or `dotnet run` reports 0 tests, check these in order:

**Missing TUnit package:**
```xml
<PackageReference Include="TUnit" Version="*" />
```

**Microsoft.NET.Test.Sdk conflict** — remove it, it conflicts with TUnit's platform:
```xml
<!-- Remove this -->
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

**Missing `[Test]` attribute:**
```csharp
[Test]
public async Task MyTest() { }
```

**Non-public or static test methods** — test methods must be public instance methods.

**Wrong OutputType** — if you see `hostfxr.dll could not be found`, check your `.csproj`:
```xml
<OutputType>Exe</OutputType>
```

## IDE Setup

TUnit requires `Microsoft.Testing.Platform` support to be enabled in your IDE.

**Visual Studio:** Tools > Options > Preview Features > enable "Use testing platform server mode", then restart.

**Rider:** Settings > Build, Execution, Deployment > Unit Testing > Testing Platform > enable "Testing Platform support", then restart.

**VS Code:** Install C# Dev Kit, then set `"dotnet.testWindow.useTestingPlatformProtocol": true` in settings, then reload.

If tests still don't appear after enabling, try a clean rebuild. In Visual Studio, deleting the `.vs` folder can help.

## Test Filtering

TUnit uses tree-node filter syntax, not the VSTest filter syntax.

**Pattern:** `/Assembly/Namespace/Class/Method[Property=Value]`

```bash
# All tests in a class
dotnet run -- --treenode-filter "/*/*/MyTestClass/*"

# A specific test method
dotnet run -- --treenode-filter "/*/*/MyTestClass/MyTestMethod"

# By category
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Exclude a category
dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# Multiple filters (OR)
dotnet run -- --treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"

# Combine filters (AND)
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration][Priority=High]"
```

## AOT Compilation Errors

If you see trim warnings or "source generator did not generate" errors, make sure you're using AOT-compatible data sources:

```csharp
// Reflection-based — may cause AOT issues
[MethodDataSource(typeof(DataClass), "GetData")]

// AOT-friendly generic version
[MethodDataSource<DataClass>(nameof(DataClass.GetData))]
```

## InstanceMethodDataSource Returns No Tests

If you're using `InstanceMethodDataSource` with a `ClassDataSource` fixture that implements `IAsyncInitializer`, tests won't appear during discovery. The fixture hasn't been initialised yet at discovery time, so the data source returns nothing.

The fix is to return predefined identifiers that don't depend on initialisation:

```csharp
public class Fixture : IAsyncInitializer
{
    private static readonly string[] TestCaseIds = ["Case1", "Case2", "Case3"];

    public async Task InitializeAsync()
    {
        await StartDockerContainerAsync();
    }

    public IEnumerable<string> GetTestCaseIds() => TestCaseIds;
}
```

## Hooks Not Running

Class-level and assembly-level hooks must be static:

```csharp
// Won't work — instance method
[Before(Class)]
public void ClassSetup() { }

// Works
[Before(Class)]
public static void ClassSetup() { }
```

Test-level hooks (`[Before(Test)]` / `[After(Test)]`) can be instance methods.

## Code Coverage

The `TUnit` meta package includes `Microsoft.Testing.Extensions.CodeCoverage` automatically. If you're using `TUnit.Engine` directly, add it manually:

```xml
<PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" Version="*" />
```

**Basic usage:**
```bash
dotnet run --configuration Release --coverage
dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

If you have Coverlet installed from a previous framework, remove it — `coverlet.collector` and `coverlet.msbuild` are not compatible with TUnit.

## Getting Help

1. Search [GitHub Issues](https://github.com/thomhurst/TUnit/issues) for similar problems
2. Run with `--diagnostic` for detailed logs
3. If it's a bug, open an issue with your TUnit version, .NET version, and a minimal reproduction
