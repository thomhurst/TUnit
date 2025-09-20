# CI/CD Reporting

TUnit provides built-in integration with continuous integration and deployment platforms, automatically detecting and adapting to various CI environments.

## GitHub Actions Reporter

When running in GitHub Actions, TUnit automatically generates a test summary that appears in the workflow run summary. This feature activates when the `GITHUB_ACTIONS` environment variable is detected.

### Automatic Detection

The GitHub reporter automatically enables when:
- The `GITHUB_ACTIONS` environment variable is set
- The `GITHUB_STEP_SUMMARY` environment variable points to a valid file

No additional configuration is required for basic functionality.

### Reporter Styles

TUnit's GitHub reporter supports two output styles:

#### Collapsible Style (Default)

The collapsible style provides a clean, concise summary with expandable details:

```markdown
### TestAssembly (.NET 9.0)

| Test Count | Status |
| --- | --- |
| 95 | Passed |
| 5 | Failed |

<details>
<summary>📊 Test Details (click to expand)</summary>

### Details
| Test | Status | Details | Duration |
| --- | --- | --- | --- |
| MyTest.ShouldFail | Failed | Expected: true, Actual: false | 123ms |
| ... | ... | ... | ... |

</details>
```

This is the **default style** as of TUnit v1.0.0, designed to:
- Keep workflow summaries clean and navigable
- Allow quick overview of test results
- Provide full details on demand
- Work well with large test suites

#### Full Style (Legacy)

The full style displays all test details directly in the summary:

```markdown
### TestAssembly (.NET 9.0)

| Test Count | Status |
| --- | --- |
| 95 | Passed |
| 5 | Failed |

### Details
| Test | Status | Details | Duration |
| --- | --- | --- | --- |
| MyTest.ShouldFail | Failed | Expected: true, Actual: false | 123ms |
| ... | ... | ... | ... |
```

### Configuration Options

You can control the GitHub reporter style using either command-line arguments or environment variables:

#### Command Line

```bash
# Use collapsible style (default)
dotnet test -- --github-reporter-style collapsible

# Use full style (legacy behavior)
dotnet test -- --github-reporter-style full
```

#### Environment Variable

```bash
# Set reporter style via environment variable
export TUNIT_GITHUB_REPORTER_STYLE=collapsible  # or 'full'
dotnet test
```

```yaml
# In GitHub Actions workflow
- name: Run Tests
  env:
    TUNIT_GITHUB_REPORTER_STYLE: collapsible
  run: dotnet test
```

### GitHub Actions Workflow Example

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Run Tests with Collapsible Reporter
      run: dotnet test --logger "console;verbosity=normal"
      # GitHub reporter auto-detects and uses collapsible style by default
    
    - name: Run Tests with Full Reporter
      run: dotnet test -- --github-reporter-style full
      # Explicitly use full style for all details
```

### Disabling the GitHub Reporter

If you need to disable the GitHub reporter entirely:

#### Via Environment Variable

```bash
export TUNIT_DISABLE_GITHUB_REPORTER=true
# or
export DISABLE_GITHUB_REPORTER=true
```

```yaml
# In GitHub Actions workflow
- name: Run Tests Without GitHub Reporter
  env:
    TUNIT_DISABLE_GITHUB_REPORTER: true
  run: dotnet test
```

### File Size Limitations

The GitHub reporter respects GitHub's file size limits:
- Maximum summary file size: 1MB
- If the output would exceed this limit, the reporter will skip writing to prevent errors
- Consider using the collapsible style for large test suites to reduce summary size

### Filtering Test Output

When using the GitHub reporter, only non-passing tests are included in the details section:
- Failed tests
- Skipped tests
- Timed out tests
- Cancelled tests
- Tests that never completed (in progress)

Passed tests are counted but not listed in details to keep the summary focused on actionable items.

## Other CI Platforms

While TUnit currently provides specialized support for GitHub Actions, it works with all CI/CD platforms through standard test output formats:

### Azure DevOps

Use TRX reporting for Azure DevOps integration:

```bash
dotnet test --report-trx --report-trx-filename TestResults.trx
```

### Jenkins

Jenkins can consume various test output formats:

```bash
# Generate TRX report for Jenkins
dotnet test --report-trx

# Or use console output with appropriate verbosity
dotnet test --logger "console;verbosity=detailed"
```

### GitLab CI

GitLab can parse test results in various formats:

```yaml
test:
  script:
    - dotnet test --report-trx
  artifacts:
    reports:
      junit:
        - TestResults/*.trx
```

## Environment Detection

TUnit automatically detects common CI environments through environment variables:

| Platform | Detection Variables |
| --- | --- |
| GitHub Actions | `GITHUB_ACTIONS` |
| Azure DevOps | `AZURE_PIPELINES` |
| Jenkins | `JENKINS_URL` |
| GitLab CI | `GITLAB_CI` |
| CircleCI | `CIRCLECI` |
| Travis CI | `TRAVIS` |
| AppVeyor | `APPVEYOR` |
| TeamCity | `TEAMCITY_VERSION` |
| Generic CI | `CI`, `CONTINUOUS_INTEGRATION` |

This detection helps TUnit optimize its behavior for each platform.

## Best Practices

1. **Use Collapsible Style for Large Test Suites**: The default collapsible style keeps summaries manageable
2. **Filter Tests in CI**: Use `--treenode-filter` to run specific test subsets in different CI jobs
3. **Set Appropriate Timeouts**: Use `--timeout` to prevent hanging builds
4. **Enable Fail-Fast for Quick Feedback**: Use `--fail-fast` in PR validation builds
5. **Customize Verbosity**: Adjust `--output` based on your debugging needs

## Troubleshooting

### GitHub Reporter Not Working

If the GitHub reporter isn't generating summaries:

1. **Check Environment Variables**:
   ```bash
   echo $GITHUB_ACTIONS  # Should output 'true'
   echo $GITHUB_STEP_SUMMARY  # Should point to a file
   ```

2. **Verify Not Disabled**:
   ```bash
   # Ensure these are not set
   echo $TUNIT_DISABLE_GITHUB_REPORTER
   echo $DISABLE_GITHUB_REPORTER
   ```

3. **Check File Permissions**: Ensure the process can write to `GITHUB_STEP_SUMMARY` file

4. **Review Test Output**: Run with `--diagnostic` to see detailed logs

### Summary Not Appearing

If tests run but no summary appears:

- Check if all tests passed (summaries may be minimal for all-passing runs)
- Verify the workflow has completed (summaries appear after job completion)
- Check the "Summary" section of the GitHub Actions run page

### Large Summary Files

If you encounter file size issues:

- Switch to collapsible style: `--github-reporter-style collapsible`
- Filter tests to reduce output: `--treenode-filter "critical-tests"`
- Run tests in separate jobs with focused test subsets