# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 426.3 ns     | 0.97 ns     | 0.91 ns     | 2.34 KB   |
| Imposter        | 617.9 ns     | 4.40 ns     | 4.12 ns     | 6.12 KB   |
| Mockolate       | 251.5 ns     | 0.69 ns     | 0.58 ns     | 1.41 KB   |
| Moq             | 234,233.4 ns | 1,486.40 ns | 1,317.65 ns | 28.56 KB  |
| NSubstitute     | 4,587.9 ns   | 30.31 ns    | 28.35 ns    | 9.01 KB   |
| FakeItEasy      | 5,523.2 ns   | 11.02 ns    | 9.20 ns     | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 636.5 ns    | 2.99 ns   | 2.49 ns   | 3.15 KB   |
| Imposter        | 1,328.1 ns  | 3.01 ns   | 2.81 ns   | 10.59 KB  |
| Mockolate       | 442.1 ns    | 3.12 ns   | 2.92 ns   | 2.35 KB   |
| Moq             | 68,027.6 ns | 350.70 ns | 292.85 ns | 16.53 KB  |
| NSubstitute     | 8,695.7 ns  | 45.89 ns  | 40.68 ns  | 20.31 KB  |
| FakeItEasy      | 5,168.4 ns  | 44.14 ns  | 39.13 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-20T02:41:11.657Z*
