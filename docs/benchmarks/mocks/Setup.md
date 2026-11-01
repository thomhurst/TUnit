# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.403

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 576.1 ns     | 5.52 ns     | 5.17 ns     | 2.38 KB   |
| Imposter        | 361.3 ns     | 1.94 ns     | 1.72 ns     | 1.42 KB   |
| Mockolate       | 324.3 ns     | 3.30 ns     | 3.09 ns     | 1.41 KB   |
| Moq             | 427,006.6 ns | 3,632.43 ns | 2,835.96 ns | 28.52 KB  |
| NSubstitute     | 6,312.5 ns   | 38.01 ns    | 33.70 ns    | 9.01 KB   |
| FakeItEasy      | 8,430.8 ns   | 49.01 ns    | 45.85 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 817.0 ns     | 8.45 ns     | 7.90 ns     | 3.2 KB    |
| Imposter        | 658.1 ns     | 9.15 ns     | 8.56 ns     | 2.63 KB   |
| Mockolate       | 574.3 ns     | 4.97 ns     | 4.40 ns     | 2.35 KB   |
| Moq             | 115,433.6 ns | 1,086.21 ns | 1,016.04 ns | 16.35 KB  |
| NSubstitute     | 13,151.5 ns  | 119.84 ns   | 112.10 ns   | 20.47 KB  |
| FakeItEasy      | 7,977.2 ns   | 119.25 ns   | 93.11 ns    | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-01T02:27:32.250Z*
