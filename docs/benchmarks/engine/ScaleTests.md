# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-10-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev     |
| --------------- | ------- | ----------- | ----------- | ---------- |
| **TUnit**       | 1.73.19 | 372.95 ms   | 356.55 ms   | 56.687 ms  |
| NUnit           | 5.0.0   | 786.49 ms   | 753.90 ms   | 125.520 ms |
| MSTest          | 4.5.1   | 738.51 ms   | 744.71 ms   | 97.450 ms  |
| xUnit3          | 4.0.2   | 1,044.30 ms | 1,056.73 ms | 79.469 ms  |
| **TUnit (AOT)** | 1.73.19 | 29.56 ms    | 29.56 ms    | 2.397 ms   |
| xUnit3\_AOT     | 4.0.2   | 38.71 ms    | 38.79 ms    | 4.150 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-10-11T00:39:42.545Z*
