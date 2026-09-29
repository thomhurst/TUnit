# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev     |
| --------------- | ------- | ----------- | ----------- | ---------- |
| **TUnit**       | 1.72.0  | 430.30 ms   | 419.21 ms   | 45.091 ms  |
| NUnit           | 5.0.0   | 1,456.47 ms | 1,469.65 ms | 124.883 ms |
| MSTest          | 4.4.1   | 1,516.00 ms | 1,522.63 ms | 58.976 ms  |
| xUnit3          | 4.0.1   | 1,226.54 ms | 1,224.75 ms | 82.128 ms  |
| **TUnit (AOT)** | 1.72.0  | 78.89 ms    | 79.42 ms    | 2.655 ms   |
| xUnit3\_AOT     | 4.0.1   | 198.00 ms   | 198.85 ms   | 3.889 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T18:13:18.340Z*
