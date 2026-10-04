# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev     |
| --------------- | ------- | ----------- | ----------- | ---------- |
| **TUnit**       | 1.72.16 | 450.57 ms   | 432.23 ms   | 72.587 ms  |
| NUnit           | 5.0.0   | 1,367.91 ms | 1,362.72 ms | 126.629 ms |
| MSTest          | 4.4.1   | 1,216.92 ms | 1,196.18 ms | 61.888 ms  |
| xUnit3          | 4.0.1   | 923.67 ms   | 922.54 ms   | 77.232 ms  |
| **TUnit (AOT)** | 1.72.16 | 74.28 ms    | 73.38 ms    | 3.987 ms   |
| xUnit3\_AOT     | 4.0.1   | 185.14 ms   | 182.91 ms   | 6.941 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T01:16:39.757Z*
