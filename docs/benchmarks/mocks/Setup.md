# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 430.8 ns     | 3.08 ns     | 2.57 ns     | 2.37 KB   |
| Imposter        | 620.9 ns     | 6.77 ns     | 5.65 ns     | 6.09 KB   |
| Mockolate       | 256.0 ns     | 3.18 ns     | 2.66 ns     | 1.41 KB   |
| Moq             | 237,545.5 ns | 2,113.98 ns | 1,977.42 ns | 28.56 KB  |
| NSubstitute     | 4,702.9 ns   | 79.40 ns    | 74.27 ns    | 9.01 KB   |
| FakeItEasy      | 5,663.7 ns   | 50.53 ns    | 44.80 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 640.3 ns    | 8.03 ns   | 7.12 ns   | 3.18 KB   |
| Imposter        | 1,101.8 ns  | 20.38 ns  | 19.06 ns  | 10.55 KB  |
| Mockolate       | 446.7 ns    | 6.44 ns   | 6.02 ns   | 2.35 KB   |
| Moq             | 67,653.7 ns | 231.54 ns | 205.25 ns | 16.35 KB  |
| NSubstitute     | 8,761.9 ns  | 165.68 ns | 162.72 ns | 20.34 KB  |
| FakeItEasy      | 5,281.2 ns  | 79.00 ns  | 61.68 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-07T02:41:59.908Z*
