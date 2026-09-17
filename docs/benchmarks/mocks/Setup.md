# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 499.9 ns     | 2.87 ns     | 2.54 ns     | 2.34 KB   |
| Imposter        | 779.1 ns     | 5.97 ns     | 5.59 ns     | 6.12 KB   |
| Mockolate       | 313.1 ns     | 6.32 ns     | 7.99 ns     | 1.41 KB   |
| Moq             | 437,619.8 ns | 3,372.39 ns | 2,989.53 ns | 28.67 KB  |
| NSubstitute     | 6,439.4 ns   | 112.61 ns   | 129.68 ns   | 9.01 KB   |
| FakeItEasy      | 8,622.4 ns   | 126.94 ns   | 112.53 ns   | 10.6 KB   |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 780.0 ns     | 8.33 ns   | 7.79 ns   | 3.15 KB   |
| Imposter        | 1,348.4 ns   | 24.91 ns  | 27.69 ns  | 10.59 KB  |
| Mockolate       | 525.3 ns     | 10.44 ns  | 12.82 ns  | 2.35 KB   |
| Moq             | 116,564.2 ns | 422.99 ns | 374.97 ns | 16.67 KB  |
| NSubstitute     | 12,541.9 ns  | 154.42 ns | 144.45 ns | 20.31 KB  |
| FakeItEasy      | 7,975.1 ns   | 155.34 ns | 145.31 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-17T02:33:27.459Z*
