# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev    |
| --------------- | ------- | --------- | --------- | --------- |
| **TUnit**       | 1.68.17 | 317.80 ms | 316.58 ms | 5.447 ms  |
| NUnit           | 4.6.1   | 588.01 ms | 584.35 ms | 16.332 ms |
| MSTest          | 4.4.1   | 536.90 ms | 537.86 ms | 7.500 ms  |
| xUnit3          | 4.0.1   | 648.73 ms | 645.63 ms | 10.657 ms |
| **TUnit (AOT)** | 1.68.17 | 20.73 ms  | 20.53 ms  | 2.060 ms  |
| xUnit3\_AOT     | 4.0.1   | 26.37 ms  | 26.33 ms  | 2.742 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-20T00:35:50.558Z*
