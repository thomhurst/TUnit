# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-11-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.403

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev    |
| --------------- | ------- | ----------- | ----------- | --------- |
| **TUnit**       | 1.76.28 | 334.52 ms   | 334.62 ms   | 1.968 ms  |
| NUnit           | 5.0.0   | 1,177.50 ms | 1,178.76 ms | 12.181 ms |
| MSTest          | 4.5.2   | 1,092.32 ms | 1,089.57 ms | 6.846 ms  |
| xUnit3          | 4.0.2   | 748.72 ms   | 747.43 ms   | 6.006 ms  |
| **TUnit (AOT)** | 1.76.28 | 65.53 ms    | 65.47 ms    | 0.409 ms  |
| xUnit3\_AOT     | 4.0.2   | 171.25 ms   | 171.48 ms   | 0.759 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-11-01T00:31:22.051Z*
