# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 532.4 ns     | 10.07 ns    | 13.09 ns    | 2.34 KB   |
| Imposter        | 683.9 ns     | 13.69 ns    | 17.80 ns    | 6.12 KB   |
| Mockolate       | 301.6 ns     | 6.11 ns     | 7.03 ns     | 1.41 KB   |
| Moq             | 188,613.6 ns | 2,188.59 ns | 1,940.13 ns | 28.46 KB  |
| NSubstitute     | 5,683.3 ns   | 70.05 ns    | 65.53 ns    | 9.01 KB   |
| FakeItEasy      | 5,505.9 ns   | 108.67 ns   | 120.79 ns   | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 794.2 ns    | 11.82 ns  | 11.06 ns  | 3.15 KB   |
| Imposter        | 1,238.8 ns  | 24.65 ns  | 32.05 ns  | 10.59 KB  |
| Mockolate       | 532.8 ns    | 10.67 ns  | 11.42 ns  | 2.35 KB   |
| Moq             | 52,347.2 ns | 175.87 ns | 155.91 ns | 16.52 KB  |
| NSubstitute     | 9,298.8 ns  | 109.32 ns | 102.26 ns | 20.31 KB  |
| FakeItEasy      | 5,422.6 ns  | 106.62 ns | 122.78 ns | 11.78 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-11T02:38:48.126Z*
