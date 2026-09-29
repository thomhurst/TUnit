# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev    |
| --------------- | ------- | ----------- | ----------- | --------- |
| **TUnit**       | 1.71.0  | 333.92 ms   | 333.59 ms   | 3.834 ms  |
| NUnit           | 5.0.0   | 1,159.77 ms | 1,159.62 ms | 11.077 ms |
| MSTest          | 4.4.1   | 1,160.92 ms | 1,145.45 ms | 42.741 ms |
| xUnit3          | 4.0.1   | 934.09 ms   | 930.53 ms   | 67.294 ms |
| **TUnit (AOT)** | 1.71.0  | 72.37 ms    | 72.10 ms    | 2.172 ms  |
| xUnit3\_AOT     | 4.0.1   | 180.99 ms   | 180.98 ms   | 2.194 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T16:54:28.495Z*
