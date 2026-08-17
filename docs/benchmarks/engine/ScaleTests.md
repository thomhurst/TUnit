# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev    |
| --------------- | ------- | --------- | --------- | --------- |
| **TUnit**       | 1.65.0  | 343.40 ms | 340.79 ms | 22.201 ms |
| NUnit           | 4.6.1   | 551.49 ms | 546.91 ms | 32.588 ms |
| MSTest          | 4.3.3   | 549.90 ms | 547.70 ms | 29.407 ms |
| xUnit3          | 4.0.0   | 725.09 ms | 717.83 ms | 63.212 ms |
| **TUnit (AOT)** | 1.65.0  | 22.19 ms  | 22.12 ms  | 1.050 ms  |
| xUnit3\_AOT     | 4.0.0   | 26.04 ms  | 26.14 ms  | 0.610 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T16:25:00.425Z*
