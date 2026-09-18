# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 333.5 ns     | 6.70 ns     | 6.58 ns     | 2.34 KB   |
| Imposter        | 439.8 ns     | 5.41 ns     | 4.51 ns     | 6.12 KB   |
| Mockolate       | 192.2 ns     | 3.77 ns     | 6.09 ns     | 1.41 KB   |
| Moq             | 137,618.8 ns | 2,255.01 ns | 1,883.04 ns | 28.57 KB  |
| NSubstitute     | 3,278.1 ns   | 51.94 ns    | 48.58 ns    | 9.06 KB   |
| FakeItEasy      | 3,870.2 ns   | 29.27 ns    | 22.85 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev      | Allocated |
| --------------- | ----------- | --------- | ----------- | --------- |
| **TUnit.Mocks** | 472.6 ns    | 5.97 ns   | 4.99 ns     | 3.15 KB   |
| Imposter        | 702.5 ns    | 7.62 ns   | 6.76 ns     | 10.59 KB  |
| Mockolate       | 312.6 ns    | 4.12 ns   | 3.44 ns     | 2.35 KB   |
| Moq             | 35,057.1 ns | 687.04 ns | 1,273.48 ns | 16.53 KB  |
| NSubstitute     | 6,228.1 ns  | 122.14 ns | 175.18 ns   | 20.5 KB   |
| FakeItEasy      | 3,762.9 ns  | 47.82 ns  | 42.39 ns    | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-18T02:32:22.133Z*
