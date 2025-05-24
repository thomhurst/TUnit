---
sidebar_position: 1
---

# Extensions

As TUnit is built on top of Microsoft.Testing.Platform, it can tap into generic testing extension packages.
Here will list a few of them.

## Code Coverage
Code coverage is provided via the `Microsoft.Testing.Extensions.CodeCoverage` NuGet package.

Install:
```
dotnet add package Microsoft.Testing.Extensions.CodeCoverage
```
Then you can run your tests with the `--coverage` flag.
```
dotnet run --configuration Release --coverage
```

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-code-coverage)

## TRX Test Reports
Trx reports are provided via the `Microsoft.Testing.Extensions.TrxReport` NuGet package.

Install:
```
dotnet add package Microsoft.Testing.Extensions.TrxReport
```
Then you can run your tests with the `--report-trx` flag.
```
dotnet run --configuration Release --report-trx
```

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-test-reports)

## Crash Dump
Crash dump is an extension to help diagnose unexplained crashes, provided via the `Microsoft.Testing.Extensions.CrashDump` NuGet package.

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-diagnostics)

## Hang Dump
Hang dump is an extension to help diagnose unexplained hanging in your test suite, provided via the `Microsoft.Testing.Extensions.HangDump` NuGet package.

[More information can be found here](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-extensions-diagnostics)
