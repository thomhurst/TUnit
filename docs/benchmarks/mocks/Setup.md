# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 399.5 ns     | 1.00 ns     | 0.89 ns     | 2.37 KB   |
| Imposter        | 592.4 ns     | 10.18 ns    | 17.56 ns    | 6.12 KB   |
| Mockolate       | 223.1 ns     | 1.36 ns     | 1.06 ns     | 1.41 KB   |
| Moq             | 157,880.1 ns | 1,905.47 ns | 1,782.38 ns | 28.65 KB  |
| NSubstitute     | 4,136.6 ns   | 33.28 ns    | 27.79 ns    | 9.01 KB   |
| FakeItEasy      | 4,074.3 ns   | 28.66 ns    | 26.80 ns    | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 606.6 ns    | 1.21 ns   | 1.13 ns   | 3.18 KB   |
| Imposter        | 983.0 ns    | 5.10 ns   | 4.26 ns   | 10.59 KB  |
| Mockolate       | 392.5 ns    | 0.66 ns   | 0.62 ns   | 2.35 KB   |
| Moq             | 39,601.3 ns | 177.59 ns | 157.43 ns | 16.33 KB  |
| NSubstitute     | 6,921.2 ns  | 45.07 ns  | 39.95 ns  | 20.49 KB  |
| FakeItEasy      | 3,971.7 ns  | 40.77 ns  | 38.14 ns  | 11.7 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-03T02:34:47.768Z*
