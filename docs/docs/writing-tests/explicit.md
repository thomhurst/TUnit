# Explicit

If you want a test to only be run explicitly (and not part of all general tests) then you can add the `[Explicit]` attribute.

This can be added to a test method or a test class.

A test is considered 'explicitly' run when all filtered tests have an explicit attribute on them.

That means that you could run all tests in a class with an `[Explicit]` attribute. Or you could run a single method with an `[Explicit]` attribute. But if you try to run a mix of explicit and non-explicit tests, then the ones with an `[Explicit]` attribute will be excluded from the run.

This can be useful for tests that make sense in a local environment but not as part of CI builds, or for helper utilities that should be easily runnable without affecting the overall test suite.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Explicit]
    public async Task Seed_Local_Database()
    {
        await Database.SeedAsync();

        await Assert.That(await Database.CountAsync()).IsGreaterThan(0);
    }
}
```

## Running Explicit Tests from the Command Line

Use a treenode filter that selects only the explicit test by name or class:

```bash
dotnet run -- --treenode-filter "/*/*/MyTestClass/Seed_Local_Database"
```

Because every test matched by the filter has `[Explicit]`, TUnit will run them.

## Combining `[Explicit]` with `[Category]`

Use `[Category]` to group explicit tests so they can be filtered by property:

```csharp
public class DevUtilities
{
    [Test]
    [Explicit]
    [Category("DevTool")]
    public async Task Warm_Up_Cache()
    {
        await CacheService.WarmUpAsync();

        await Assert.That(CacheService.IsWarmed).IsTrue();
    }

    [Test]
    [Explicit]
    [Category("DevTool")]
    public async Task Reset_Feature_Flags()
    {
        await FeatureFlags.ResetAllAsync();

        await Assert.That(await FeatureFlags.CountAsync()).IsEqualTo(0);
    }
}
```

Run all explicit dev tools at once:

```bash
dotnet run -- --treenode-filter "/*/*/*/*[Category=DevTool]"
```
