# Installing TUnit

## Easily

Assuming you have the .NET SDK installed, simply run:

`dotnet new install TUnit.Templates`

`dotnet new TUnit -n "YourProjectName"`

A new test project will be created for you with some samples of different test types and tips. When you're ready to get going, delete them and create your own!

## Manually

First create an empty .NET console application:

```powershell
dotnet new console --name YourTestProjectNameHere
```

To that project add the `TUnit` package:

```powershell
cd YourTestProjectNameHere
dotnet add package TUnit --prerelease
```

And then remove any automatically generated `Program.cs` or main method, as this'll be taken care of by the TUnit package.

### Global Usings

The TUnit package automatically configures global usings for common TUnit namespaces, so your test files don't need to include using statements for:

- `TUnit.Core` (for `[Test]` attribute)
- `TUnit.Assertions` (for `Assert.That()`)
- `TUnit.Assertions.Extensions` (for assertion methods)

This means your test files can be as simple as:

```csharp
namespace MyTests;

public class MyTests  // No [TestClass] needed!
{
    [Test]  // Available without explicit using statement
    public async Task MyTest()
    {
        await Assert.That(true).IsTrue();  // Assert is available automatically
    }
}
```

### What's Included in the TUnit Package

When you install the **TUnit** meta package, you automatically get several useful extensions without any additional installation:

#### ✅ Built-In Extensions

**Microsoft.Testing.Extensions.CodeCoverage**
- 📊 Code coverage support via `--coverage` flag
- 📈 Outputs Cobertura and XML formats
- 🔄 Replacement for Coverlet (which is **not compatible** with TUnit)

**Microsoft.Testing.Extensions.TrxReport**
- 📝 TRX test report generation via `--report-trx` flag
- 🤝 Compatible with Azure DevOps and other CI/CD systems

This means you can run tests with coverage and reports right away:

```bash
# Run tests with code coverage
dotnet run --configuration Release --coverage

# Run tests with TRX report
dotnet run --configuration Release --report-trx

# Both coverage and report
dotnet run --configuration Release --coverage --report-trx
```

**Important:** Do **not** install `coverlet.collector` or `coverlet.msbuild`. These packages are incompatible with TUnit because they require the VSTest platform, while TUnit uses the modern Microsoft.Testing.Platform.

For more details, see:
- [Code Coverage Documentation](../extensions/extensions.md#code-coverage)
- [Extensions Overview](../extensions/extensions.md)

That's it. We're ready to write our first test.

Your `.csproj` should be as simple as something like:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="TUnit" Version="*" />
    </ItemGroup>

</Project>
```

:::danger

If you're used to other testing frameworks, you're probably used to the package `Microsoft.NET.Test.Sdk`.
This should NOT be used with TUnit. It'll stop test discovery from working properly.

:::

## .NET Framework

### Automatic Polyfill Support
When targeting .NET Framework or older .NET Standard versions (netstandard2.0/2.1), TUnit automatically provides the [Polyfill](https://github.com/SimonCropp/Polyfill) package as a transitive dependency. This package adds missing types that are required for modern C# features and TUnit's code generation, such as:

- `ModuleInitializerAttribute` - Used for test discovery initialization
- `CallerArgumentExpressionAttribute` - Used for assertion messages
- Various other modern .NET types

The Polyfill package flows automatically from `TUnit.Core` to your test projects, so you don't need to add it manually.

### Central Package Management (CPM)
TUnit is fully compatible with NuGet Central Package Management. The Polyfill dependency is managed automatically as a transitive dependency.

If you prefer to manage the Polyfill version yourself:
- Add `<PackageVersion Include="Polyfill" Version="x.x.x" />` to your `Directory.Packages.props`
- TUnit will respect your version choice

### Embedded Polyfill Attributes
TUnit automatically sets `<PolyUseEmbeddedAttribute>true</PolyUseEmbeddedAttribute>` in your projects. This ensures that each project gets its own embedded copy of polyfilled types with `internal` visibility. This approach:

- **Prevents type conflicts** when using `InternalsVisibleTo`
- **Isolates types** per-assembly (no public API pollution)
- **Follows best practices** from the [Polyfill documentation](https://github.com/SimonCropp/Polyfill/blob/main/consuming.md#recommended-consuming-pattern)

You can override this behavior if needed:
```xml
<PropertyGroup>
    <PolyUseEmbeddedAttribute>false</PolyUseEmbeddedAttribute>
</PropertyGroup>
```

Note: This property is set in TUnit's `.props` file which runs before your project file, so your settings always take precedence.
