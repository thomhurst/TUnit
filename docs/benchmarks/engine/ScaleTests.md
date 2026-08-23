# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev    |
| --------------- | ------- | --------- | --------- | --------- |
| **TUnit**       | 1.65.38 | 280.02 ms | 279.97 ms | 3.282 ms  |
| NUnit           | 4.6.1   | 522.04 ms | 515.34 ms | 22.927 ms |
| MSTest          | 4.3.3   | 505.40 ms | 504.41 ms | 12.702 ms |
| xUnit3          | 4.0.0   | 620.62 ms | 618.35 ms | 15.701 ms |
| **TUnit (AOT)** | 1.65.38 | 19.86 ms  | 20.26 ms  | 2.114 ms  |
| xUnit3\_AOT     | 4.0.0   | 23.07 ms  | 22.85 ms  | 0.874 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-08-23T00:20:42.433Z*
