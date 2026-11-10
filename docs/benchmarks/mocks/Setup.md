# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.403

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 600.4 ns     | 11.80 ns    | 13.11 ns    | 2.38 KB   |
| Imposter        | 358.9 ns     | 7.23 ns     | 10.60 ns    | 1.35 KB   |
| Mockolate       | 347.8 ns     | 7.02 ns     | 11.92 ns    | 1.41 KB   |
| Moq             | 430,130.3 ns | 3,865.56 ns | 3,426.72 ns | 28.52 KB  |
| NSubstitute     | 6,458.3 ns   | 118.92 ns   | 116.80 ns   | 9.06 KB   |
| FakeItEasy      | 8,386.4 ns   | 77.45 ns    | 68.65 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 819.4 ns     | 14.35 ns    | 15.35 ns    | 3.2 KB    |
| Imposter        | 629.2 ns     | 11.95 ns    | 12.27 ns    | 2.52 KB   |
| Mockolate       | 593.9 ns     | 8.43 ns     | 7.04 ns     | 2.35 KB   |
| Moq             | 114,874.5 ns | 1,290.76 ns | 1,207.38 ns | 16.35 KB  |
| NSubstitute     | 12,746.2 ns  | 48.15 ns    | 45.04 ns    | 20.31 KB  |
| FakeItEasy      | 8,152.3 ns   | 154.12 ns   | 189.27 ns   | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-10T02:26:34.876Z*
