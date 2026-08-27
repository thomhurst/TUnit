# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 538.0 ns     | 4.23 ns     | 3.96 ns     | 2.34 KB   |
| Imposter        | 877.2 ns     | 7.83 ns     | 6.94 ns     | 6.12 KB   |
| Mockolate       | 316.5 ns     | 3.79 ns     | 3.17 ns     | 1.41 KB   |
| Moq             | 425,654.3 ns | 3,016.20 ns | 2,821.35 ns | 28.52 KB  |
| NSubstitute     | 6,329.6 ns   | 25.81 ns    | 22.88 ns    | 9.01 KB   |
| FakeItEasy      | 8,285.4 ns   | 22.88 ns    | 20.28 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 791.6 ns     | 3.81 ns     | 3.56 ns   | 3.15 KB   |
| Imposter        | 1,395.6 ns   | 5.33 ns     | 4.72 ns   | 10.59 KB  |
| Mockolate       | 576.9 ns     | 10.78 ns    | 10.08 ns  | 2.35 KB   |
| Moq             | 116,085.0 ns | 1,006.02 ns | 941.04 ns | 16.61 KB  |
| NSubstitute     | 12,317.6 ns  | 129.13 ns   | 114.47 ns | 20.34 KB  |
| FakeItEasy      | 7,688.7 ns   | 136.75 ns   | 127.91 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-27T04:05:27.840Z*
