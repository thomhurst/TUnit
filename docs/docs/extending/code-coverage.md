---
sidebar_position: 2
title: Code Coverage
---

# Code Coverage

TUnit includes built-in code coverage support via `Microsoft.Testing.Extensions.CodeCoverage`, which is automatically included when you install the **TUnit** meta package. No additional packages are required.

## Coverlet is Not Compatible

If you are migrating from another test framework and currently use **Coverlet** (`coverlet.collector` or `coverlet.msbuild`), you must remove it. TUnit uses Microsoft.Testing.Platform, not VSTest, and Coverlet only works with the legacy VSTest platform.

Remove these from your `.csproj`:

```xml
<!-- Remove these -->
<PackageReference Include="coverlet.collector" Version="x.x.x" />
<PackageReference Include="coverlet.msbuild" Version="x.x.x" />
```

## Running Coverage

Use the `--coverage` flag when running your tests:

```bash
# Basic coverage collection
dotnet run --configuration Release --coverage

# Specify output location
dotnet run --configuration Release --coverage --coverage-output ./coverage/

# Specify output format (cobertura is the default)
dotnet run --configuration Release --coverage --coverage-output-format cobertura

# Multiple formats
dotnet run --configuration Release --coverage \
  --coverage-output-format cobertura \
  --coverage-output-format xml
```

## Output Formats

The Microsoft coverage tool supports multiple output formats:

| Format | Flag | Notes |
|--------|------|-------|
| Cobertura | `--coverage-output-format cobertura` | Default. Widely supported by CI tools. |
| XML | `--coverage-output-format xml` | Visual Studio format. |

You can specify multiple `--coverage-output-format` flags to generate several formats in one run.

## Viewing Coverage Results

Coverage files are generated in your test output directory:

```
TestResults/
  ├── coverage.cobertura.xml
  └── <guid>/
      └── coverage.xml
```

Tools for viewing results:
- **Visual Studio** -- Built-in coverage viewer
- **VS Code** -- Extensions like "Coverage Gutters"
- **ReportGenerator** -- Generate HTML reports: `reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport`
- **CI Tools** -- Most CI systems can parse Cobertura format natively

## Configuration

### Coverage Settings File

You can customize coverage behavior (include/exclude modules, etc.) with a settings file:

```bash
dotnet run --configuration Release --coverage --coverage-settings coverage.config
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Run tests with coverage
  run: dotnet run --project ./tests/MyProject.Tests --configuration Release --coverage
```

### Azure Pipelines

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'run'
    arguments: '--configuration Release --coverage --coverage-output $(Agent.TempDirectory)/coverage/'
```

## Troubleshooting

**Coverage files not generated?**
- Ensure you are using the `TUnit` meta package, not just `TUnit.Engine`.
- Verify you have a recent .NET SDK installed.

**Missing coverage for some assemblies?**
- Use a `testconfig.json` file to explicitly include or exclude modules.
- See [Microsoft's configuration documentation](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md).

**Further reading:**
- [Microsoft Code Coverage extension docs](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-code-coverage)
- [Unit Testing Code Coverage Guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
