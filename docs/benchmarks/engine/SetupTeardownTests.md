# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev    |
| --------------- | ------- | ----------- | ----------- | --------- |
| **TUnit**       | 1.68.17 | 446.02 ms   | 439.30 ms   | 48.695 ms |
| NUnit           | 4.6.1   | 1,260.98 ms | 1,244.96 ms | 67.114 ms |
| MSTest          | 4.4.1   | 1,256.00 ms | 1,256.06 ms | 95.100 ms |
| xUnit3          | 4.0.1   | 1,024.87 ms | 1,028.69 ms | 82.360 ms |
| **TUnit (AOT)** | 1.68.17 | 76.32 ms    | 76.24 ms    | 2.663 ms  |
| xUnit3\_AOT     | 4.0.1   | 187.51 ms   | 186.82 ms   | 3.003 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-20T00:35:50.558Z*
