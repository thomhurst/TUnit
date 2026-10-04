# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev     |
| --------------- | ------- | --------- | --------- | ---------- |
| **TUnit**       | 1.72.16 | 410.28 ms | 400.98 ms | 50.054 ms  |
| NUnit           | 5.0.0   | 779.01 ms | 761.04 ms | 106.077 ms |
| MSTest          | 4.4.1   | 602.94 ms | 595.17 ms | 39.956 ms  |
| xUnit3          | 4.0.1   | 776.74 ms | 770.03 ms | 29.280 ms  |
| **TUnit (AOT)** | 1.72.16 | 25.61 ms  | 25.49 ms  | 2.072 ms   |
| xUnit3\_AOT     | 4.0.1   | 35.74 ms  | 35.78 ms  | 2.939 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T01:16:39.756Z*
