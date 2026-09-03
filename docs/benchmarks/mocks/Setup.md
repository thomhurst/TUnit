# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 558.0 ns     | 3.68 ns     | 3.26 ns     | 2.34 KB   |
| Imposter        | 785.8 ns     | 3.16 ns     | 2.80 ns     | 6.12 KB   |
| Mockolate       | 334.3 ns     | 3.51 ns     | 3.28 ns     | 1.41 KB   |
| Moq             | 296,655.4 ns | 1,766.59 ns | 1,475.18 ns | 28.64 KB  |
| NSubstitute     | 5,951.5 ns   | 78.92 ns    | 69.96 ns    | 9.01 KB   |
| FakeItEasy      | 7,229.0 ns   | 25.10 ns    | 23.48 ns    | 10.53 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 814.7 ns    | 4.57 ns   | 4.27 ns   | 3.15 KB   |
| Imposter        | 1,331.2 ns  | 5.71 ns   | 5.06 ns   | 10.59 KB  |
| Mockolate       | 569.8 ns    | 6.23 ns   | 5.52 ns   | 2.35 KB   |
| Moq             | 86,550.6 ns | 682.82 ns | 605.30 ns | 16.53 KB  |
| NSubstitute     | 11,253.7 ns | 144.32 ns | 135.00 ns | 20.31 KB  |
| FakeItEasy      | 7,108.7 ns  | 112.78 ns | 99.98 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-03T02:45:05.205Z*
