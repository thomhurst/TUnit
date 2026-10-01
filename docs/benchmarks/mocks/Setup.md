# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 430.0 ns     | 2.28 ns     | 1.90 ns     | 2.37 KB   |
| Imposter        | 606.4 ns     | 5.27 ns     | 4.40 ns     | 6.12 KB   |
| Mockolate       | 243.6 ns     | 0.93 ns     | 0.78 ns     | 1.41 KB   |
| Moq             | 238,264.3 ns | 1,260.16 ns | 1,117.10 ns | 28.66 KB  |
| NSubstitute     | 4,533.6 ns   | 29.09 ns    | 24.29 ns    | 9.01 KB   |
| FakeItEasy      | 5,531.1 ns   | 20.82 ns    | 18.46 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 648.3 ns    | 1.80 ns   | 1.41 ns   | 3.18 KB   |
| Imposter        | 1,050.6 ns  | 6.26 ns   | 5.86 ns   | 10.59 KB  |
| Mockolate       | 433.1 ns    | 2.49 ns   | 2.21 ns   | 2.35 KB   |
| Moq             | 68,022.5 ns | 322.11 ns | 301.30 ns | 16.35 KB  |
| NSubstitute     | 8,719.2 ns  | 39.66 ns  | 37.10 ns  | 20.31 KB  |
| FakeItEasy      | 5,270.8 ns  | 84.97 ns  | 79.48 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-01T02:49:56.169Z*
