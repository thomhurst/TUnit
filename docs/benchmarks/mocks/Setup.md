# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 428.5 ns     | 1.48 ns     | 1.31 ns     | 2.34 KB   |
| Imposter        | 666.4 ns     | 5.11 ns     | 4.78 ns     | 6.12 KB   |
| Mockolate       | 252.1 ns     | 1.84 ns     | 1.63 ns     | 1.41 KB   |
| Moq             | 239,104.4 ns | 1,811.00 ns | 1,512.27 ns | 28.56 KB  |
| NSubstitute     | 4,655.3 ns   | 24.16 ns    | 21.42 ns    | 9.01 KB   |
| FakeItEasy      | 5,611.8 ns   | 17.55 ns    | 15.56 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 624.9 ns    | 6.76 ns   | 6.32 ns   | 3.15 KB   |
| Imposter        | 1,053.9 ns  | 3.89 ns   | 3.45 ns   | 10.59 KB  |
| Mockolate       | 433.0 ns    | 5.14 ns   | 4.81 ns   | 2.35 KB   |
| Moq             | 67,862.0 ns | 377.06 ns | 334.26 ns | 16.53 KB  |
| NSubstitute     | 8,661.0 ns  | 60.18 ns  | 50.25 ns  | 20.5 KB   |
| FakeItEasy      | 5,256.8 ns  | 48.79 ns  | 43.25 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-22T02:33:33.738Z*
