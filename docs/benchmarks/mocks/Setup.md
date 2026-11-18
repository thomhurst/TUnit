# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 525.7 ns     | 10.23 ns    | 12.17 ns    | 2.38 KB   |
| Imposter        | 302.1 ns     | 5.71 ns     | 6.58 ns     | 1.33 KB   |
| Mockolate       | 298.2 ns     | 5.83 ns     | 7.37 ns     | 1.39 KB   |
| Moq             | 478,985.3 ns | 2,307.23 ns | 2,045.30 ns | 28.52 KB  |
| NSubstitute     | 6,345.5 ns   | 86.30 ns    | 80.72 ns    | 8.97 KB   |
| FakeItEasy      | 7,538.4 ns   | 139.72 ns   | 130.70 ns   | 10.14 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 743.6 ns     | 14.67 ns  | 27.55 ns  | 3.2 KB    |
| Imposter        | 550.2 ns     | 10.90 ns  | 18.81 ns  | 2.48 KB   |
| Mockolate       | 513.1 ns     | 9.94 ns   | 10.63 ns  | 2.31 KB   |
| Moq             | 113,429.3 ns | 525.08 ns | 491.16 ns | 15.89 KB  |
| NSubstitute     | 12,406.8 ns  | 108.79 ns | 96.44 ns  | 20.5 KB   |
| FakeItEasy      | 7,530.9 ns   | 100.50 ns | 94.01 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-18T02:25:26.946Z*
