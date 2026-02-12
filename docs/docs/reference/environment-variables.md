# Environment Variables

TUnit supports configuration through environment variables, allowing you to set defaults without modifying command-line arguments. This is particularly useful for CI/CD pipelines and development environments.

## TUnit-Specific Environment Variables

### TUNIT_DISABLE_LOGO

Disables the TUnit ASCII art logo when starting a test session.

```bash
# Bash/Linux/macOS
export TUNIT_DISABLE_LOGO=true

# PowerShell
$env:TUNIT_DISABLE_LOGO = "true"

# Windows Command Prompt
set TUNIT_DISABLE_LOGO=true
```

**Equivalent to:** `--disable-logo`

**Use case:** Reduces output noise in CI/CD logs or when using AI/LLM coding assistants that parse test output.

### TUNIT_DISABLE_GITHUB_REPORTER

Disables the automatic GitHub Actions test summary reporter.

```bash
export TUNIT_DISABLE_GITHUB_REPORTER=true
```

**Use case:** When you want to use a custom reporting solution instead of the built-in GitHub Actions integration.

### TUNIT_GITHUB_REPORTER_STYLE

Controls the style of the GitHub Actions test reporter output.

```bash
export TUNIT_GITHUB_REPORTER_STYLE=collapsible  # default
export TUNIT_GITHUB_REPORTER_STYLE=full
```

**Values:**
- `collapsible` (default): Wraps detailed test results in expandable HTML blocks
- `full`: Displays all test details directly

**Equivalent to:** `--github-reporter-style`

### TUNIT_DISABLE_JUNIT_REPORTER

Disables the JUnit XML reporter.

```bash
export TUNIT_DISABLE_JUNIT_REPORTER=true
```

### TUNIT_ENABLE_JUNIT_REPORTER

Explicitly enables the JUnit XML reporter.

```bash
export TUNIT_ENABLE_JUNIT_REPORTER=true
```

### JUNIT_XML_OUTPUT_PATH

Sets the output path for JUnit XML reports.

```bash
export JUNIT_XML_OUTPUT_PATH=/path/to/output.xml
```

### TUNIT_MAX_PARALLEL_TESTS

Sets the maximum number of tests that can run in parallel.

```bash
export TUNIT_MAX_PARALLEL_TESTS=4    # Limit to 4 concurrent tests
export TUNIT_MAX_PARALLEL_TESTS=0    # Unlimited parallelism
```

**Equivalent to:** `--maximum-parallel-tests`

**Note:** Command-line arguments take precedence over environment variables.

### TUNIT_ENABLE_IDE_STREAMING

Enables real-time output streaming to IDE test explorers (Rider, VS Code, Visual Studio).

```bash
# Bash/Linux/macOS
export TUNIT_ENABLE_IDE_STREAMING=1

# PowerShell
$env:TUNIT_ENABLE_IDE_STREAMING = "1"

# Windows Command Prompt
set TUNIT_ENABLE_IDE_STREAMING=1
```

**Default:** Disabled

**Use case:** When running tests in an IDE, this enables real-time streaming of test output (e.g. `Console.WriteLine`) to the test explorer while tests are still running. Without this, output is shown after each test completes.

**Note:** This feature is disabled by default due to known compatibility issues with the Microsoft Testing Platform that can cause test runner crashes in some IDEs. Enable it only if you want real-time output streaming and are not experiencing issues.

## Microsoft Testing Platform Environment Variables

These environment variables are provided by the underlying Microsoft Testing Platform:

### TESTINGPLATFORM_TELEMETRY_OPTOUT

Disables telemetry collection.

```bash
export TESTINGPLATFORM_TELEMETRY_OPTOUT=1
```

### TESTINGPLATFORM_UI_LANGUAGE

Sets the language for platform messages and logs.

```bash
export TESTINGPLATFORM_UI_LANGUAGE=en-us
```

## Platform-Level Flags

Some command-line flags are handled by the Microsoft Testing Platform rather than TUnit directly. These flags currently do not have environment variable equivalents:

- `--no-progress` - Disables progress reporting to screen
- `--no-ansi` - Disables ANSI escape characters

### Workarounds for --no-progress

While there's no environment variable for `--no-progress`, you can use these alternatives:

**1. MSBuild Property (with `dotnet test`):**

```xml
<PropertyGroup>
  <TestingPlatformCommandLineArguments>--no-progress</TestingPlatformCommandLineArguments>
</PropertyGroup>
```

**2. Shell Wrapper:**

```bash
#!/bin/bash
# run-tests.sh
./MyTestProject.exe --no-progress "$@"
```

**3. Direct Command Line:**

```bash
./MyTestProject.exe --no-progress
```

## CI/CD Examples

### GitHub Actions

```yaml
- name: Run Tests
  env:
    TUNIT_DISABLE_LOGO: true
    TUNIT_GITHUB_REPORTER_STYLE: collapsible
  run: dotnet test
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  env:
    TUNIT_DISABLE_LOGO: true
  inputs:
    command: 'test'
```

### GitLab CI

```yaml
test:
  variables:
    TUNIT_DISABLE_LOGO: "true"
  script:
    - dotnet test
```

### Docker

```dockerfile
ENV TUNIT_DISABLE_LOGO=true
ENV TUNIT_MAX_PARALLEL_TESTS=4
```

## Priority Order

When the same setting is configured in multiple places, TUnit follows this priority order (highest to lowest):

1. **Command-line arguments** - Always take precedence
2. **Environment variables** - Applied when command-line argument is not provided
3. **Configuration files** - Applied as defaults

## Summary Table

| Environment Variable | Equivalent Flag | Description |
|---------------------|-----------------|-------------|
| `TUNIT_DISABLE_LOGO` | `--disable-logo` | Disables ASCII art logo |
| `TUNIT_GITHUB_REPORTER_STYLE` | `--github-reporter-style` | GitHub reporter style |
| `TUNIT_DISABLE_GITHUB_REPORTER` | - | Disables GitHub reporter |
| `TUNIT_DISABLE_JUNIT_REPORTER` | - | Disables JUnit reporter |
| `TUNIT_ENABLE_JUNIT_REPORTER` | - | Enables JUnit reporter |
| `JUNIT_XML_OUTPUT_PATH` | - | JUnit output path |
| `TUNIT_MAX_PARALLEL_TESTS` | `--maximum-parallel-tests` | Max parallel tests |
| `TUNIT_ENABLE_IDE_STREAMING` | - | Enable real-time IDE output streaming |
