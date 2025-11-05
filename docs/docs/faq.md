# FAQ

### My test project won't execute / I get errors about runtime identifiers?

Make sure that your `OutputType` is set to `exe` in your csproj

### My project runs but no tests are discovered or executed?

Ensure you're not installing the old `Microsoft.NET.Test.Sdk` NuGet package

### I'm getting errors about conflicting main methods?

Ensure you're not installing the old `Microsoft.NET.Test.Sdk` NuGet package

### I can't see any tests in my IDE?

Make sure you've enabled Testing Platform support in your IDE's settings.
Currently, TUnit is best supported in Visual Studio 2022 (v17.9+) and JetBrains Rider (2024.1+).

### Why do I have to await all assertions? Can I use synchronous assertions?

All TUnit assertions must be awaited. There's no synchronous alternative.

**Why this design?**

TUnit's assertion library is built around async from the ground up. This means:
- All assertions work consistently, whether they're simple value checks or complex async operations
- Custom assertions can perform async work (like database queries or HTTP calls)
- No sync-over-async patterns that cause deadlocks
- Assertions can be chained without blocking

**What this means when migrating:**

You need to convert your tests to `async Task` and add `await` before assertions.

Before (xUnit/NUnit/MSTest):
```csharp
[Test]
public void MyTest()
{
    var result = Calculate(2, 3);
    Assert.Equal(5, result);
}
```

After (TUnit):
```csharp
[Test]
public async Task MyTest()
{
    var result = Calculate(2, 3);
    await Assert.That(result).IsEqualTo(5);
}
```

**Automated migration**

TUnit includes code fixers that handle most of this conversion for you:

```bash
# For xUnit
dotnet format analyzers --severity info --diagnostics TUXU0001

# For NUnit
dotnet format analyzers --severity info --diagnostics TUNU0001

# For MSTest
dotnet format analyzers --severity info --diagnostics TUMS0001
```

The code fixer converts test methods to async, adds await to assertions, and updates attribute names. It handles most common cases automatically, though you may need to adjust complex scenarios manually.

See the migration guides for step-by-step instructions:
- [xUnit migration](migration/xunit.md#using-tunits-code-fixers)
- [NUnit migration](migration/nunit.md#using-tunits-code-fixers)
- [MSTest migration](migration/mstest.md#using-tunits-code-fixers)

**What you gain**

Async assertions enable patterns that aren't possible with synchronous assertions:

```csharp
[Test]
public async Task AsyncAssertion_Example()
{
    // Await async operations in assertions
    await Assert.That(async () => await GetUserAsync(123))
        .Throws<UserNotFoundException>();

    // Chain assertions naturally
    var user = await GetUserAsync(456);
    await Assert.That(user.Email)
        .IsNotNull()
        .And.Contains("@example.com");
}
```

**Watch out for missing awaits**

The most common mistake is forgetting `await`. The compiler warns you, but the test will pass without actually running the assertion:

```csharp
// Wrong - test passes without checking anything
Assert.That(result).IsEqualTo(5);  // Returns a Task that's ignored

// Correct
await Assert.That(result).IsEqualTo(5);
```

### Does TUnit work with Coverlet for code coverage?

**No.** Coverlet (`coverlet.collector` or `coverlet.msbuild`) is **not compatible** with TUnit.

**Why?** TUnit uses the modern `Microsoft.Testing.Platform` instead of the legacy VSTest platform. Coverlet only works with VSTest.

**Solution:** Use `Microsoft.Testing.Extensions.CodeCoverage` instead, which is:
- ✅ **Automatically included** with the TUnit meta package
- ✅ Provides the same functionality as Coverlet
- ✅ Outputs Cobertura and XML formats
- ✅ Works with all major CI/CD systems

See the [Code Coverage documentation](extensions/extensions.md#code-coverage) for usage instructions.

### How do I get code coverage in TUnit?

Code coverage is **built-in** and automatically included with the TUnit package!

**Basic usage:**
```bash
dotnet run --configuration Release --coverage
```

**With output location:**
```bash
dotnet run --configuration Release --coverage --coverage-output ./coverage/
```

**Specify format (cobertura, xml, etc.):**
```bash
dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

See the [Code Coverage documentation](extensions/extensions.md#code-coverage) for advanced configuration.

### My code coverage stopped working after migrating to TUnit. What do I do?

This typically happens if you still have Coverlet packages installed.

**Fix:**
1. **Remove Coverlet** from your `.csproj`:
   ```xml
   <!-- Remove these lines -->
   <PackageReference Include="coverlet.collector" Version="x.x.x" />
   <PackageReference Include="coverlet.msbuild" Version="x.x.x" />
   ```

2. **Ensure you're using the TUnit meta package** (not just TUnit.Core):
   ```xml
   <PackageReference Include="TUnit" Version="0.x.x" />
   ```

3. **Update your commands** to use the new coverage flags:
   ```bash
   # Old (VSTest + Coverlet)
   dotnet test --collect:"XPlat Code Coverage"

   # New (TUnit + Microsoft Coverage)
   dotnet run --configuration Release --coverage
   ```

4. **Update CI/CD pipelines** to use the new commands

See the migration guides for detailed instructions:
- [xUnit Migration - Code Coverage](migration/xunit.md#code-coverage)
- [NUnit Migration - Code Coverage](migration/nunit.md#code-coverage)
- [MSTest Migration - Code Coverage](migration/mstest.md#code-coverage)

### What code coverage tool should I use with TUnit?

Use **Microsoft.Testing.Extensions.CodeCoverage**, which is:
- ✅ **Already included** with the TUnit package (no manual installation)
- ✅ Built and maintained by Microsoft
- ✅ Works seamlessly with Microsoft.Testing.Platform
- ✅ Outputs industry-standard formats (Cobertura, XML)
- ✅ Compatible with all major CI/CD systems and coverage viewers

**Do not use:**
- ❌ Coverlet (incompatible with Microsoft.Testing.Platform)

### Why don't my coverage files get generated?

**Common causes:**

1. **Using TUnit.Engine only** (without the TUnit meta package)
   - The TUnit meta package includes the coverage extension automatically
   - If using TUnit.Engine directly, you must manually install `Microsoft.Testing.Extensions.CodeCoverage`

2. **Using .NET 7 or earlier**
   - Microsoft.Testing.Platform requires .NET 8+
   - Upgrade to .NET 8 or later

See the [Code Coverage Troubleshooting](troubleshooting.md#code-coverage-issues) for more solutions.

