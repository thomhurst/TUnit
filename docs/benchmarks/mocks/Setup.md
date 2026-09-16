# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 518.9 ns     | 10.38 ns    | 16.47 ns    | 2.34 KB   |
| Imposter        | 835.0 ns     | 14.14 ns    | 12.53 ns    | 6.12 KB   |
| Mockolate       | 305.5 ns     | 6.19 ns     | 5.79 ns     | 1.41 KB   |
| Moq             | 432,752.3 ns | 1,874.19 ns | 1,565.03 ns | 28.52 KB  |
| NSubstitute     | 6,068.7 ns   | 51.14 ns    | 45.34 ns    | 9.01 KB   |
| FakeItEasy      | 8,353.9 ns   | 54.99 ns    | 51.44 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 764.1 ns     | 12.05 ns  | 15.24 ns  | 3.15 KB   |
| Imposter        | 1,338.4 ns   | 26.51 ns  | 26.03 ns  | 10.59 KB  |
| Mockolate       | 543.2 ns     | 10.71 ns  | 11.90 ns  | 2.35 KB   |
| Moq             | 114,403.9 ns | 612.10 ns | 542.61 ns | 16.67 KB  |
| NSubstitute     | 12,339.0 ns  | 134.25 ns | 125.57 ns | 20.31 KB  |
| FakeItEasy      | 7,768.3 ns   | 117.77 ns | 98.34 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-16T02:32:43.042Z*
