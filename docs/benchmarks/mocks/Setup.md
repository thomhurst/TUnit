# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 481.2 ns     | 3.14 ns     | 2.78 ns     | 2.38 KB   |
| Imposter        | 287.4 ns     | 2.51 ns     | 2.22 ns     | 1.33 KB   |
| Mockolate       | 271.3 ns     | 0.99 ns     | 0.77 ns     | 1.39 KB   |
| Moq             | 464,033.3 ns | 1,890.41 ns | 1,768.29 ns | 28.52 KB  |
| NSubstitute     | 6,023.1 ns   | 92.82 ns    | 86.82 ns    | 8.97 KB   |
| FakeItEasy      | 7,361.4 ns   | 39.15 ns    | 34.71 ns    | 10.26 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 702.0 ns     | 1.84 ns   | 1.63 ns   | 3.2 KB    |
| Imposter        | 603.2 ns     | 4.00 ns   | 3.55 ns   | 2.48 KB   |
| Mockolate       | 462.8 ns     | 2.01 ns   | 1.78 ns   | 2.31 KB   |
| Moq             | 113,814.5 ns | 186.97 ns | 174.89 ns | 16 KB     |
| NSubstitute     | 11,605.1 ns  | 119.57 ns | 106.00 ns | 20.17 KB  |
| FakeItEasy      | 7,053.3 ns   | 25.45 ns  | 22.56 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-13T02:20:37.421Z*
