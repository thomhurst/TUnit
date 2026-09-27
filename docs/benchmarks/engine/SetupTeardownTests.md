# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-09-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev     |
| --------------- | ------- | ----------- | ----------- | ---------- |
| **TUnit**       | 1.69.21 | 484.28 ms   | 485.09 ms   | 38.536 ms  |
| NUnit           | 4.6.1   | 1,298.87 ms | 1,281.90 ms | 52.894 ms  |
| MSTest          | 4.4.1   | 1,294.22 ms | 1,292.73 ms | 56.599 ms  |
| xUnit3          | 4.0.1   | 956.63 ms   | 968.28 ms   | 101.354 ms |
| **TUnit (AOT)** | 1.69.21 | 68.84 ms    | 68.86 ms    | 0.600 ms   |
| xUnit3\_AOT     | 4.0.1   | 177.81 ms   | 177.72 ms   | 0.871 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-27T00:42:59.274Z*
