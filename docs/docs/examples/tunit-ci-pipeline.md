# TUnit in CI/CD Pipelines

When using TUnit in a CI/CD pipeline, you'll want to run tests, collect results, and publish reports for visibility. This guide provides complete, production-ready pipeline configurations for popular CI/CD platforms.

The best practice is to use the .NET SDK CLI (`dotnet test` or `dotnet run`) directly to maintain full control over execution, ensure reproducibility across environments, and allow for local debugging.

> **Note**: The `--` separator is required to pass arguments to the test runner when using `dotnet test` when using SDKs older than .NET 10.

## GitHub Actions

### Complete Workflow with Matrix Strategy

This workflow tests multiple .NET versions across different operating systems, collects code coverage, and publishes results:

```yaml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    name: Test on ${{ matrix.os }} - .NET ${{ matrix.dotnet-version }}
    runs-on: ${{ matrix.os }}

    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet-version: ['8.0.x', '9.0.x']
      fail-fast: false  # Continue running other jobs if one fails

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET ${{ matrix.dotnet-version }}
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet-version }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Run tests with coverage
      run: dotnet test --configuration Release --no-build --coverage --report-trx --results-directory ./TestResults

    - name: Upload test results
      if: always()  # Run even if tests fail
      uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ matrix.os }}-${{ matrix.dotnet-version }}
        path: ./TestResults/*.trx

    - name: Upload coverage
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-${{ matrix.os }}-${{ matrix.dotnet-version }}
        path: ./TestResults/*.coverage

  publish-results:
    name: Publish Test Results
    needs: test
    runs-on: ubuntu-latest
    if: always()

    steps:
    - name: Download all test results
      uses: actions/download-artifact@v4
      with:
        pattern: test-results-*
        path: ./TestResults

    - name: Publish test results
      uses: EnricoMi/publish-unit-test-result-action@v2
      with:
        files: ./TestResults/**/*.trx
```

### Workflow with AOT Testing

Test your code with Native AOT compilation:

```yaml
name: AOT Tests

on: [push, pull_request]

jobs:
  test-aot:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Restore
      run: dotnet restore

    - name: Publish with AOT
      run: dotnet publish -c Release -p:PublishAot=true --use-current-runtime

    - name: Run AOT tests
      run: ./bin/Release/net9.0/linux-x64/publish/YourTestProject
```

### PR Comment with Test Results

Post test results as a comment on pull requests:

```yaml
name: PR Tests

on:
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    permissions:
      pull-requests: write  # Required to comment on PRs

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Run tests
      run: dotnet test --configuration Release --report-trx --results-directory ./TestResults

    - name: Comment PR with results
      if: always()
      uses: EnricoMi/publish-unit-test-result-action@v2
      with:
        files: ./TestResults/*.trx
        comment_mode: always
```

## Azure DevOps

### Complete Pipeline with Stages

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pr:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetSdkVersion: '9.0.x'

stages:
- stage: Build
  displayName: 'Build Stage'
  jobs:
  - job: Build
    displayName: 'Build Job'
    steps:
    - task: UseDotNet@2
      displayName: 'Install .NET SDK'
      inputs:
        packageType: 'sdk'
        version: '$(dotnetSdkVersion)'

    - script: dotnet restore
      displayName: 'Restore dependencies'

    - script: dotnet build --configuration $(buildConfiguration) --no-restore
      displayName: 'Build solution'

    - script: dotnet publish --configuration $(buildConfiguration) --no-build --output $(Build.ArtifactStagingDirectory)
      displayName: 'Publish artifacts'

    - publish: $(Build.ArtifactStagingDirectory)
      artifact: drop
      displayName: 'Publish build artifacts'

- stage: Test
  displayName: 'Test Stage'
  dependsOn: Build
  jobs:
  - job: Test
    displayName: 'Run Tests'
    steps:
    - task: UseDotNet@2
      displayName: 'Install .NET SDK'
      inputs:
        packageType: 'sdk'
        version: '$(dotnetSdkVersion)'

    - script: dotnet restore
      displayName: 'Restore dependencies'

    - script: dotnet build --configuration $(buildConfiguration) --no-restore
      displayName: 'Build solution'

    - script: |
        dotnet test --configuration $(buildConfiguration) --no-build \
          --coverage --coverage-output-format cobertura \
          --report-trx --results-directory $(Agent.TempDirectory)
      displayName: 'Run tests with coverage'
      continueOnError: true

    - task: PublishTestResults@2
      displayName: 'Publish test results'
      condition: always()
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/*.trx'
        searchFolder: '$(Agent.TempDirectory)'
        failTaskOnFailedTests: true
        testRunTitle: 'TUnit Test Results'

    - task: PublishCodeCoverageResults@2
      displayName: 'Publish code coverage'
      condition: always()
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

### Multi-Platform Testing Matrix

```yaml
strategy:
  matrix:
    linux_net8:
      vmImage: 'ubuntu-latest'
      dotnetVersion: '8.0.x'
    linux_net9:
      vmImage: 'ubuntu-latest'
      dotnetVersion: '9.0.x'
    windows_net8:
      vmImage: 'windows-latest'
      dotnetVersion: '8.0.x'
    windows_net9:
      vmImage: 'windows-latest'
      dotnetVersion: '9.0.x'
    macos_net9:
      vmImage: 'macos-latest'
      dotnetVersion: '9.0.x'

pool:
  vmImage: $(vmImage)

steps:
- task: UseDotNet@2
  inputs:
    version: '$(dotnetVersion)'

- script: dotnet test --configuration Release --report-trx
  displayName: 'Run tests on $(vmImage) with .NET $(dotnetVersion)'
```

## GitLab CI

### Complete Pipeline with Stages

```yaml
image: mcr.microsoft.com/dotnet/sdk:9.0

variables:
  BUILD_CONFIGURATION: Release
  COVERAGE_THRESHOLD: 80

stages:
  - build
  - test
  - report

before_script:
  - dotnet --version

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build --configuration $BUILD_CONFIGURATION --no-restore
  artifacts:
    paths:
      - "*/bin/$BUILD_CONFIGURATION/"
      - "*/obj/$BUILD_CONFIGURATION/"
    expire_in: 1 hour

test:unit:
  stage: test
  dependencies:
    - build
  script:
    - dotnet test --configuration $BUILD_CONFIGURATION --no-build
      --coverage --coverage-output-format cobertura
      --report-trx --results-directory ./TestResults
  coverage: '/Total\s+\|\s+(\d+\.?\d*)%/'
  artifacts:
    when: always
    paths:
      - TestResults/
    reports:
      junit: TestResults/*.trx
      coverage_report:
        coverage_format: cobertura
        path: TestResults/coverage.cobertura.xml

test:integration:
  stage: test
  dependencies:
    - build
  script:
    - dotnet test --configuration $BUILD_CONFIGURATION --no-build
      --filter "Category=Integration"
      --report-trx --results-directory ./TestResults
  artifacts:
    when: always
    paths:
      - TestResults/
    reports:
      junit: TestResults/*.trx

coverage-report:
  stage: report
  dependencies:
    - test:unit
  script:
    - dotnet tool install -g dotnet-reportgenerator-globaltool
    - reportgenerator
      "-reports:TestResults/coverage.cobertura.xml"
      "-targetdir:coverage"
      "-reporttypes:Html;Badges"
    - echo "Coverage report generated"
  coverage: '/Total\s+\|\s+(\d+\.?\d*)%/'
  artifacts:
    paths:
      - coverage/
```

### Matrix Testing Multiple .NET Versions

```yaml
.test-template:
  stage: test
  script:
    - dotnet test --configuration Release --report-trx

test:net8:
  extends: .test-template
  image: mcr.microsoft.com/dotnet/sdk:8.0

test:net9:
  extends: .test-template
  image: mcr.microsoft.com/dotnet/sdk:9.0
```

## CircleCI

### Complete Configuration

```yaml
version: 2.1

orbs:
  dotnet: circleci/dotnet@2.0

executors:
  dotnet-executor:
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:9.0

jobs:
  build:
    executor: dotnet-executor
    steps:
      - checkout

      - run:
          name: Restore dependencies
          command: dotnet restore

      - run:
          name: Build
          command: dotnet build --configuration Release --no-restore

      - persist_to_workspace:
          root: .
          paths:
            - "*/bin/Release/"
            - "*/obj/Release/"

  test:
    executor: dotnet-executor
    steps:
      - checkout

      - attach_workspace:
          at: .

      - run:
          name: Run tests
          command: |
            dotnet test --configuration Release --no-build \
              --coverage --coverage-output-format cobertura \
              --report-trx --results-directory ./TestResults

      - run:
          name: Process test results
          when: always
          command: |
            dotnet tool install -g trx2junit
            trx2junit TestResults/*.trx

      - store_test_results:
          path: ./TestResults

      - store_artifacts:
          path: ./TestResults
          destination: test-results

  test-matrix:
    parameters:
      dotnet-version:
        type: string
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:<< parameters.dotnet-version >>
    steps:
      - checkout
      - run: dotnet test --configuration Release

workflows:
  version: 2
  build-and-test:
    jobs:
      - build
      - test:
          requires:
            - build
      - test-matrix:
          name: test-net8
          dotnet-version: "8.0"
      - test-matrix:
          name: test-net9
          dotnet-version: "9.0"
```

## Docker

### Dockerfile for Running Tests

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/**/*.csproj ./src/
COPY tests/**/*.csproj ./tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build
RUN dotnet build --configuration Release --no-restore

# Run tests
FROM build AS test
WORKDIR /src
RUN dotnet test --configuration Release --no-build \
    --coverage --report-trx --results-directory /testresults

# Export test results
FROM scratch AS export
COPY --from=test /testresults /
```

### Docker Compose for Integration Tests

```yaml
version: '3.8'

services:
  tests:
    build:
      context: .
      dockerfile: Dockerfile
      target: test
    environment:
      - DOTNET_ENVIRONMENT=Test
      - ConnectionStrings__Database=Host=postgres;Database=testdb;Username=test;Password=test
    depends_on:
      - postgres
      - redis
    volumes:
      - ./TestResults:/testresults

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: testdb
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"
```

Run tests with:

```bash
docker-compose up --build tests
```

## Best Practices

### Separate Restore, Build, and Test

For efficiency and clarity, separate these steps:

```yaml
# GitHub Actions
- name: Restore
  run: dotnet restore

- name: Build
  run: dotnet build --no-restore --configuration Release

- name: Test
  run: dotnet test --no-build --configuration Release
```

### Use Caching

Cache NuGet packages to speed up builds:

```yaml
# GitHub Actions
- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

### Parallel Test Execution

Configure parallelism based on available resources:

```bash
# Default: Use all available cores
dotnet test

# Limit parallelism in resource-constrained environments
dotnet test -- --maximum-parallel-tests 4
```

### Filter Tests by Category

Run different test categories in separate jobs:

```yaml
# Unit tests (fast)
- name: Unit Tests
  run: dotnet test --filter "Category=Unit"

# Integration tests (slower)
- name: Integration Tests
  run: dotnet test --filter "Category=Integration"
```

### Fail Fast in PRs

Use fail-fast mode for quick feedback in pull requests:

```bash
dotnet test --fail-fast
```

## Troubleshooting

### Tests Timing Out

Increase the test timeout:

```bash
dotnet test -- --timeout 5m  # 5 minutes
```

### Coverage Files Not Generated

Ensure you're using the TUnit meta package (not just TUnit.Engine):

```xml
<PackageReference Include="TUnit" Version="*" />
```

### Out of Memory in CI

Limit parallel test execution:

```bash
dotnet test -- --maximum-parallel-tests 2
```

Or increase the heap size:

```bash
export DOTNET_GCHeapHardLimit=0x40000000  # 1GB
dotnet test
```

