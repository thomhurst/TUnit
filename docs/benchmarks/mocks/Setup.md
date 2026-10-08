# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 683.1 ns     | 2.08 ns     | 1.84 ns   | 2.37 KB   |
| Imposter        | 1,037.4 ns   | 4.08 ns     | 3.62 ns   | 6.09 KB   |
| Mockolate       | 397.8 ns     | 0.95 ns     | 0.89 ns   | 1.41 KB   |
| Moq             | 211,746.6 ns | 1,046.50 ns | 927.70 ns | 28.46 KB  |
| NSubstitute     | 6,392.7 ns   | 64.05 ns    | 59.91 ns  | 9.01 KB   |
| FakeItEasy      | 6,395.1 ns   | 15.07 ns    | 13.36 ns  | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1,007.7 ns  | 3.95 ns   | 3.50 ns   | 3.18 KB   |
| Imposter        | 1,837.5 ns  | 6.33 ns   | 5.92 ns   | 10.55 KB  |
| Mockolate       | 671.3 ns    | 2.77 ns   | 2.59 ns   | 2.35 KB   |
| Moq             | 58,465.1 ns | 570.37 ns | 505.62 ns | 16.33 KB  |
| NSubstitute     | 10,950.0 ns | 73.71 ns  | 65.34 ns  | 20.49 KB  |
| FakeItEasy      | 6,226.3 ns  | 29.04 ns  | 25.74 ns  | 11.7 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-08T02:41:51.713Z*
