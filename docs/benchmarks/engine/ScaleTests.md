# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-08-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev    |
| --------------- | ------- | --------- | --------- | --------- |
| **TUnit**       | 1.65.68 | 334.54 ms | 334.15 ms | 24.989 ms |
| NUnit           | 4.6.1   | 643.81 ms | 636.45 ms | 32.013 ms |
| MSTest          | 4.3.3   | 562.39 ms | 560.91 ms | 32.264 ms |
| xUnit3          | 4.0.0   | 710.91 ms | 708.11 ms | 30.502 ms |
| **TUnit (AOT)** | 1.65.68 | 18.88 ms  | 18.84 ms  | 0.550 ms  |
| xUnit3\_AOT     | 4.0.0   | 23.38 ms  | 23.59 ms  | 1.044 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-08-30T00:32:59.982Z*
