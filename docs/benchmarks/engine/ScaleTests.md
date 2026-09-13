# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-09-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev    |
| --------------- | ------- | --------- | --------- | --------- |
| **TUnit**       | 1.67.0  | 455.88 ms | 446.74 ms | 63.526 ms |
| NUnit           | 4.6.1   | 733.90 ms | 732.85 ms | 64.841 ms |
| MSTest          | 4.4.0   | 702.74 ms | 703.00 ms | 80.853 ms |
| xUnit3          | 4.0.1   | 892.54 ms | 886.93 ms | 79.339 ms |
| **TUnit (AOT)** | 1.67.0  | 26.41 ms  | 26.77 ms  | 2.482 ms  |
| xUnit3\_AOT     | 4.0.1   | 33.95 ms  | 33.82 ms  | 2.526 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-13T00:36:18.780Z*
