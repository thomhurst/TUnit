# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 369.7 ns    | 2.37 ns   | 2.10 ns   | 3.12 KB   |
| Imposter        | 268.4 ns    | 5.39 ns   | 10.12 ns  | 2.64 KB   |
| Mockolate       | 197.1 ns    | 1.70 ns   | 1.50 ns   | 1.8 KB    |
| Moq             | 52,968.0 ns | 299.01 ns | 249.69 ns | 13.29 KB  |
| NSubstitute     | 2,261.8 ns  | 33.66 ns  | 29.84 ns  | 7.85 KB   |
| FakeItEasy      | 2,424.4 ns  | 12.78 ns  | 9.98 ns   | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 461.9 ns    | 1.53 ns   | 1.28 ns   | 3.21 KB   |
| Imposter        | 301.8 ns    | 4.42 ns   | 4.13 ns   | 2.8 KB    |
| Mockolate       | 233.0 ns    | 4.36 ns   | 4.28 ns   | 1.84 KB   |
| Moq             | 55,440.5 ns | 372.77 ns | 348.69 ns | 13.72 KB  |
| NSubstitute     | 2,536.8 ns  | 17.46 ns  | 16.33 ns  | 8.41 KB   |
| FakeItEasy      | 2,835.1 ns  | 17.05 ns  | 13.31 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-11T02:43:35.245Z*
