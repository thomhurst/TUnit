# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 581.8 ns     | 2.23 ns     | 2.09 ns     | 2.34 KB   |
| Imposter        | 799.2 ns     | 3.43 ns     | 3.21 ns     | 6.12 KB   |
| Mockolate       | 326.3 ns     | 2.43 ns     | 2.27 ns     | 1.41 KB   |
| Moq             | 298,620.6 ns | 2,000.39 ns | 1,773.29 ns | 28.86 KB  |
| NSubstitute     | 5,828.4 ns   | 14.53 ns    | 12.88 ns    | 9.01 KB   |
| FakeItEasy      | 7,003.6 ns   | 30.88 ns    | 27.38 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 818.8 ns    | 2.58 ns   | 2.29 ns   | 3.15 KB   |
| Imposter        | 1,335.3 ns  | 3.40 ns   | 2.84 ns   | 10.59 KB  |
| Mockolate       | 557.8 ns    | 1.64 ns   | 1.54 ns   | 2.35 KB   |
| Moq             | 87,763.4 ns | 512.74 ns | 454.53 ns | 16.53 KB  |
| NSubstitute     | 11,340.9 ns | 56.32 ns  | 49.93 ns  | 20.31 KB  |
| FakeItEasy      | 6,621.3 ns  | 37.28 ns  | 33.04 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-25T02:41:00.074Z*
