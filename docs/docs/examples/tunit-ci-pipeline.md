# TUnit in CI pipelines

When using TUnit for end-to-end (E2E) tests or TUnit's Playwright library for UI testing, you will likely run these tests in a CI/CD pipeline—either on a schedule or as part of a release. In such cases, it is important to publish the test results for visibility and reporting.

The best practice is to use the .NET SDK CLI (dotnet test) directly to maintain full control over execution, ensure reproducibility across environments, and allow for local debugging.

Below is an example of how to execute and publish TUnit test results to Azure DevOps Test Runs.

> Note: The -- separator is required to pass arguments to the test runner.

```yaml
steps:
  - script: dotnet test --configuration Release -- --report-trx --results-directory $(Agent.TempDirectory)
    displayName: 'Run tests and output .trx file'
    continueOnError: true

  - task: PublishTestResults@2
    displayName: 'Publish Test Results from *.trx files'
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '*.trx'
      searchFolder: '$(Agent.TempDirectory)'
      failTaskOnFailedTests: true
      failTaskOnMissingResultsFile: true
```
> Best Practice:  
> For efficiency and clarity in failures, separate restore, build, and test into distinct steps.  
> A common approach is to perform restore and build in a "build pipeline", then execute tests using --no-build in a separate "test pipeline" to avoid redundant compilation and improve performance.

