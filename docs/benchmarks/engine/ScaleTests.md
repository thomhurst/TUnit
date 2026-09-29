# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev     |
| --------------- | ------- | ----------- | ----------- | ---------- |
| **TUnit**       | 1.71.0  | 455.86 ms   | 416.93 ms   | 110.214 ms |
| NUnit           | 5.0.0   | 721.99 ms   | 707.07 ms   | 78.368 ms  |
| MSTest          | 4.4.1   | 726.62 ms   | 713.95 ms   | 94.749 ms  |
| xUnit3          | 4.0.1   | 1,017.40 ms | 1,010.46 ms | 104.138 ms |
| **TUnit (AOT)** | 1.71.0  | 31.10 ms    | 31.20 ms    | 4.098 ms   |
| xUnit3\_AOT     | 4.0.1   | 41.38 ms    | 41.20 ms    | 6.093 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T16:54:28.495Z*
