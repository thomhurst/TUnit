# ScaleTests Benchmark

> Large test suites (150+ tests) measuring scalability

Last Updated

This benchmark was automatically generated on **2026-09-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean      | Median    | StdDev     |
| --------------- | ------- | --------- | --------- | ---------- |
| **TUnit**       | 1.69.21 | 480.55 ms | 479.51 ms | 59.015 ms  |
| NUnit           | 4.6.1   | 736.26 ms | 742.84 ms | 77.199 ms  |
| MSTest          | 4.4.1   | 645.30 ms | 633.48 ms | 64.840 ms  |
| xUnit3          | 4.0.1   | 949.20 ms | 942.15 ms | 149.225 ms |
| **TUnit (AOT)** | 1.69.21 | 33.10 ms  | 33.93 ms  | 4.168 ms   |
| xUnit3\_AOT     | 4.0.1   | 37.08 ms  | 37.04 ms  | 3.580 ms   |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-09-27T00:42:59.274Z*
