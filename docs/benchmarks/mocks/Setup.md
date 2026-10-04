# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 437.1 ns     | 5.49 ns     | 5.13 ns   | 2.37 KB   |
| Imposter        | 647.9 ns     | 10.24 ns    | 9.08 ns   | 6.12 KB   |
| Mockolate       | 251.0 ns     | 4.17 ns     | 3.70 ns   | 1.41 KB   |
| Moq             | 242,848.4 ns | 1,072.53 ns | 837.36 ns | 28.66 KB  |
| NSubstitute     | 4,753.3 ns   | 50.39 ns    | 42.08 ns  | 9.01 KB   |
| FakeItEasy      | 5,904.7 ns   | 63.49 ns    | 59.39 ns  | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 654.0 ns    | 8.70 ns   | 8.14 ns   | 3.18 KB   |
| Imposter        | 1,091.6 ns  | 21.12 ns  | 22.60 ns  | 10.59 KB  |
| Mockolate       | 445.7 ns    | 4.71 ns   | 4.41 ns   | 2.35 KB   |
| Moq             | 68,349.3 ns | 235.96 ns | 209.17 ns | 16.35 KB  |
| NSubstitute     | 8,620.8 ns  | 111.32 ns | 98.68 ns  | 20.31 KB  |
| FakeItEasy      | 5,334.7 ns  | 103.42 ns | 96.74 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T03:10:54.658Z*
