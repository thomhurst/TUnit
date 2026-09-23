# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 545.1 ns     | 4.79 ns     | 4.48 ns     | 2.34 KB   |
| Imposter        | 840.0 ns     | 11.21 ns    | 10.49 ns    | 6.12 KB   |
| Mockolate       | 313.4 ns     | 3.72 ns     | 3.48 ns     | 1.41 KB   |
| Moq             | 437,587.5 ns | 1,739.59 ns | 1,627.22 ns | 28.52 KB  |
| NSubstitute     | 6,241.3 ns   | 31.38 ns    | 27.81 ns    | 9.01 KB   |
| FakeItEasy      | 8,742.5 ns   | 71.24 ns    | 66.64 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 838.4 ns     | 10.05 ns    | 8.91 ns     | 3.15 KB   |
| Imposter        | 1,597.0 ns   | 31.36 ns    | 44.98 ns    | 10.59 KB  |
| Mockolate       | 575.4 ns     | 11.38 ns    | 14.80 ns    | 2.35 KB   |
| Moq             | 117,616.8 ns | 1,102.81 ns | 1,031.57 ns | 16.53 KB  |
| NSubstitute     | 12,707.1 ns  | 150.49 ns   | 140.77 ns   | 20.31 KB  |
| FakeItEasy      | 8,095.0 ns   | 94.42 ns    | 83.70 ns    | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-23T02:34:56.618Z*
