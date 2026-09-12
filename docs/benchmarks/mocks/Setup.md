# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 522.0 ns     | 4.97 ns     | 4.41 ns     | 2.34 KB   |
| Imposter        | 818.2 ns     | 6.18 ns     | 5.48 ns     | 6.12 KB   |
| Mockolate       | 295.9 ns     | 2.73 ns     | 2.55 ns     | 1.41 KB   |
| Moq             | 435,371.9 ns | 2,308.64 ns | 2,046.55 ns | 28.52 KB  |
| NSubstitute     | 6,280.6 ns   | 22.29 ns    | 20.85 ns    | 9.06 KB   |
| FakeItEasy      | 7,993.2 ns   | 21.85 ns    | 20.44 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 751.3 ns     | 2.36 ns     | 1.97 ns     | 3.15 KB   |
| Imposter        | 1,462.9 ns   | 7.90 ns     | 7.01 ns     | 10.59 KB  |
| Mockolate       | 526.4 ns     | 2.81 ns     | 2.63 ns     | 2.35 KB   |
| Moq             | 118,335.6 ns | 1,142.86 ns | 1,069.03 ns | 16.53 KB  |
| NSubstitute     | 12,698.8 ns  | 150.56 ns   | 140.84 ns   | 20.31 KB  |
| FakeItEasy      | 8,060.7 ns   | 90.56 ns    | 84.71 ns    | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-12T02:33:29.738Z*
