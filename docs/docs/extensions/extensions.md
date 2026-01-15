# Extensions

As TUnit is built on top of Microsoft.Testing.Platform, it can tap into generic testing extension packages.

## Built-In Extensions

The following extensions are **automatically included** when you install the **TUnit** meta package:

- **Code Coverage** - via `Microsoft.Testing.Extensions.CodeCoverage`
- **TRX Test Reports** - via `Microsoft.Testing.Extensions.TrxReport`
- **Telemetry** - via `Microsoft.Testing.Extensions.Telemetry`

:::tip Opting Out of Built-In Extensions
If you don't want these extensions, you can reference `TUnit.Engine` and `TUnit.Assertions` packages separately instead of the `TUnit` meta package.

```xml
<!-- Instead of the TUnit meta package -->
<PackageReference Include="TUnit.Engine" Version="x.x.x" />
<PackageReference Include="TUnit.Assertions" Version="x.x.x" />
```
:::

### Code Coverage

Code coverage is provided via the `Microsoft.Testing.Extensions.CodeCoverage` NuGet package.

**✅ Included automatically with the TUnit package** - No manual installation needed!

#### Usage

Run your tests with the `--coverage` flag:
```bash
# Basic coverage
dotnet run --configuration Release --coverage

# Specify output location
dotnet run --configuration Release --coverage --coverage-output ./coverage/

# Specify output format (cobertura is default)
dotnet run --configuration Release --coverage --coverage-output-format cobertura

# Multiple formats
dotnet run --configuration Release --coverage \
  --coverage-output-format cobertura \
  --coverage-output-format xml
```

#### Important: Coverlet Incompatibility ⚠️

**If you're migrating from xUnit, NUnit, or MSTest:**

- **Remove Coverlet** (`coverlet.collector` or `coverlet.msbuild`) from your project
- TUnit uses Microsoft.Testing.Platform (not VSTest), which is incompatible with Coverlet
- Microsoft.Testing.Extensions.CodeCoverage is the modern replacement and provides the same functionality

**Migration Example:**
```xml
<!-- Remove from your .csproj -->
<PackageReference Include="coverlet.collector" Version="x.x.x" />
<PackageReference Include="coverlet.msbuild" Version="x.x.x" />

<!-- Already included with TUnit meta package -->
<PackageReference Include="TUnit" Version="0.x.x" />
```

See the migration guides for detailed instructions:
- [xUnit Migration Guide - Code Coverage](../migration/xunit.md#code-coverage)
- [NUnit Migration Guide - Code Coverage](../migration/nunit.md#code-coverage)
- [MSTest Migration Guide - Code Coverage](../migration/mstest.md#code-coverage)

#### Advanced Configuration

You can customize coverage with a `.runsettings` file:

**coverage.runsettings:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*\.dll$</ModulePath>
              </Include>
              <Exclude>
                <ModulePath>.*tests\.dll$</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

**Use it:**
```bash
dotnet run --configuration Release --coverage --coverage-settings coverage.runsettings
```

**📚 More Resources:**
- [Microsoft's Code Coverage Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-code-coverage)
- [Unit Testing Code Coverage Guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

---

### TRX Test Reports

TRX reports are provided via the `Microsoft.Testing.Extensions.TrxReport` NuGet package.

**✅ Included automatically with the TUnit package** - No manual installation needed!

#### Usage

Run your tests with the `--report-trx` flag:
```bash
# Generate TRX report
dotnet run --configuration Release --report-trx

# Specify output location
dotnet run --configuration Release --results-directory ./reports --report-trx --report-trx-filename testresults.trx
```

**📚 More Resources:**
- [Microsoft's TRX Report Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-test-reports)

---

### Telemetry

Telemetry is provided via the `Microsoft.Testing.Extensions.Telemetry` NuGet package.

**✅ Included automatically with the TUnit package**

This extension enables Microsoft to collect anonymous usage metrics to help improve the testing platform. No personal data or source code is collected.

#### Opting Out

You can disable telemetry by setting an environment variable:

```bash
# Linux/macOS
export TESTINGPLATFORM_TELEMETRY_OPTOUT=1

# Windows
set TESTINGPLATFORM_TELEMETRY_OPTOUT=1
```

Alternatively, you can use `DOTNET_CLI_TELEMETRY_OPTOUT=1` which also disables .NET SDK telemetry.

**📚 More Resources:**
- [Microsoft.Testing.Platform Telemetry Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-telemetry)

---

## Optional Extensions

These extensions are **not** included with the TUnit package and must be installed manually if needed.

## Crash Dump
Crash dump is an extension to help diagnose unexplained crashes, provided via the `Microsoft.Testing.Extensions.CrashDump` NuGet package.

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-diagnostics)

## Hang Dump
Hang dump is an extension to help diagnose unexplained hanging in your test suite, provided via the `Microsoft.Testing.Extensions.HangDump` NuGet package.

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-diagnostics)
