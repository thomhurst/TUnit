# SetupTeardownTests Benchmark

> Expensive test fixtures with setup/teardown overhead

Last Updated

This benchmark was automatically generated on **2026-10-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402

## 📊 Results[​](#-results "Direct link to 📊 Results")

| Framework       | Version | Mean        | Median      | StdDev    |
| --------------- | ------- | ----------- | ----------- | --------- |
| **TUnit**       | 1.75.0  | 480.42 ms   | 476.98 ms   | 48.695 ms |
| NUnit           | 5.0.0   | 1,368.52 ms | 1,372.37 ms | 55.640 ms |
| MSTest          | 4.5.1   | 1,313.38 ms | 1,309.99 ms | 54.842 ms |
| xUnit3          | 4.0.2   | 1,050.98 ms | 1,055.70 ms | 73.564 ms |
| **TUnit (AOT)** | 1.75.0  | 76.99 ms    | 77.25 ms    | 2.867 ms  |
| xUnit3\_AOT     | 4.0.2   | 186.77 ms   | 187.31 ms   | 4.088 ms  |

## 📈 Visual Comparison[​](#-visual-comparison "Direct link to 📈 Visual Comparison")

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3, xUnit3\_AOT using identical test scenarios.

***

Methodology

View the [benchmarks overview](/docs/benchmarks/.md) for methodology details and environment information.

*Last generated: 2026-10-18T00:34:22.693Z*
