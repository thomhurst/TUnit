# Libraries

When building a reusable library that defines shared hooks, custom attributes, base classes, or data sources for TUnit, reference **`TUnit.Core`** instead of the main `TUnit` package. The `TUnit` package configures a project as an executable test suite; `TUnit.Core` provides all the models and attributes needed for authoring test infrastructure without the test runner wiring.

## When to Build a TUnit Library

- Shared lifecycle hooks (e.g., database setup, authentication) used across multiple test projects
- Custom attributes that encapsulate common test metadata or behavior
- Reusable data sources for parameterized tests
- Base test classes with standard setup and teardown logic

## Example `.csproj` for a TUnit Library

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit.Core" />
  </ItemGroup>

</Project>
```

This produces a class library (`.dll`), not an executable.

## Example Library Code

```csharp
using TUnit.Core;
using static TUnit.Core.HookType;

namespace MyCompany.Testing;

public abstract class DatabaseTestBase
{
    [Before(Test)]
    public async Task ResetDatabase()
    {
        await TestDatabase.ResetAsync();
    }

    [After(Test)]
    public async Task CleanupConnections()
    {
        await TestDatabase.CloseConnectionsAsync();
    }
}
```

## Consuming the Library

In a test project, reference both `TUnit` (for the runner) and the library project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" />
    <ProjectReference Include="..\MyCompany.Testing\MyCompany.Testing.csproj" />
  </ItemGroup>

</Project>
```

Tests then inherit from the shared base class:

```csharp
using MyCompany.Testing;

public class OrderTests : DatabaseTestBase
{
    [Test]
    public async Task Order_Is_Created_Successfully()
    {
        var order = await OrderService.CreateAsync("item-1");

        await Assert.That(order.Id).IsNotNull();
    }
}
```
