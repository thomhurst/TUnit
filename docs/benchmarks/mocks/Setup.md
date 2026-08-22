# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 545.2 ns     | 7.79 ns     | 7.29 ns     | 2.34 KB   |
| Imposter        | 849.8 ns     | 17.01 ns    | 28.88 ns    | 6.12 KB   |
| Mockolate       | 323.3 ns     | 6.46 ns     | 6.34 ns     | 1.41 KB   |
| Moq             | 439,529.6 ns | 2,210.71 ns | 1,959.74 ns | 28.63 KB  |
| NSubstitute     | 6,234.2 ns   | 75.30 ns    | 70.44 ns    | 9.06 KB   |
| FakeItEasy      | 8,091.1 ns   | 76.60 ns    | 71.65 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 801.5 ns     | 11.71 ns  | 10.96 ns  | 3.15 KB   |
| Imposter        | 1,414.3 ns   | 27.51 ns  | 32.75 ns  | 10.59 KB  |
| Mockolate       | 554.8 ns     | 10.85 ns  | 12.50 ns  | 2.35 KB   |
| Moq             | 114,836.0 ns | 738.61 ns | 654.76 ns | 16.53 KB  |
| NSubstitute     | 12,917.8 ns  | 244.66 ns | 228.85 ns | 20.5 KB   |
| FakeItEasy      | 8,231.9 ns   | 161.58 ns | 151.14 ns | 11.79 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-22T02:40:44.558Z*
