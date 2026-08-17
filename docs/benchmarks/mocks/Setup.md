# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 498.5 ns     | 9.97 ns   | 13.98 ns  | 2.34 KB   |
| Imposter        | 616.3 ns     | 7.74 ns   | 7.24 ns   | 6.12 KB   |
| Mockolate       | 297.3 ns     | 5.95 ns   | 5.27 ns   | 1.41 KB   |
| Moq             | 178,037.1 ns | 899.08 ns | 841.00 ns | 28.64 KB  |
| NSubstitute     | 5,487.3 ns   | 74.62 ns  | 62.31 ns  | 9.01 KB   |
| FakeItEasy      | 5,275.9 ns   | 76.59 ns  | 71.64 ns  | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 722.1 ns    | 9.36 ns   | 8.76 ns   | 3.15 KB   |
| Imposter        | 1,174.1 ns  | 12.83 ns  | 11.38 ns  | 10.59 KB  |
| Mockolate       | 496.6 ns    | 6.33 ns   | 5.28 ns   | 2.35 KB   |
| Moq             | 48,501.5 ns | 307.22 ns | 272.34 ns | 16.52 KB  |
| NSubstitute     | 8,482.3 ns  | 116.50 ns | 108.98 ns | 20.31 KB  |
| FakeItEasy      | 4,910.6 ns  | 97.20 ns  | 111.93 ns | 11.7 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T02:43:20.076Z*
