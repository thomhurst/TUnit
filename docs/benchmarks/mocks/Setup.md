# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 511.2 ns     | 6.53 ns     | 6.11 ns     | 2.38 KB   |
| Imposter        | 298.3 ns     | 5.74 ns     | 5.09 ns     | 1.33 KB   |
| Mockolate       | 287.6 ns     | 5.41 ns     | 5.06 ns     | 1.39 KB   |
| Moq             | 468,103.7 ns | 1,905.68 ns | 1,782.57 ns | 28.52 KB  |
| NSubstitute     | 6,020.4 ns   | 108.24 ns   | 101.25 ns   | 8.97 KB   |
| FakeItEasy      | 7,735.5 ns   | 127.54 ns   | 119.30 ns   | 10.14 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 789.4 ns     | 13.93 ns  | 13.03 ns  | 3.2 KB    |
| Imposter        | 559.5 ns     | 9.61 ns   | 8.99 ns   | 2.48 KB   |
| Mockolate       | 539.1 ns     | 10.18 ns  | 16.73 ns  | 2.31 KB   |
| Moq             | 113,335.7 ns | 600.12 ns | 561.36 ns | 15.89 KB  |
| NSubstitute     | 12,012.4 ns  | 125.29 ns | 111.07 ns | 20.14 KB  |
| FakeItEasy      | 7,470.3 ns   | 44.31 ns  | 41.45 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-11T02:20:11.802Z*
