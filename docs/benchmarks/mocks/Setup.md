# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 551.7 ns     | 10.96 ns    | 21.89 ns    | 2.34 KB   |
| Imposter        | 851.7 ns     | 16.20 ns    | 24.25 ns    | 6.12 KB   |
| Mockolate       | 331.7 ns     | 6.55 ns     | 10.19 ns    | 1.41 KB   |
| Moq             | 433,988.1 ns | 3,886.68 ns | 3,635.60 ns | 28.68 KB  |
| NSubstitute     | 6,263.8 ns   | 76.62 ns    | 63.98 ns    | 9.01 KB   |
| FakeItEasy      | 8,319.2 ns   | 152.28 ns   | 142.44 ns   | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 774.0 ns     | 15.36 ns  | 21.53 ns  | 3.15 KB   |
| Imposter        | 1,444.0 ns   | 22.32 ns  | 18.63 ns  | 10.59 KB  |
| Mockolate       | 548.7 ns     | 10.31 ns  | 9.64 ns   | 2.35 KB   |
| Moq             | 114,312.6 ns | 812.13 ns | 719.93 ns | 16.53 KB  |
| NSubstitute     | 12,314.6 ns  | 76.22 ns  | 63.64 ns  | 20.31 KB  |
| FakeItEasy      | 7,900.2 ns   | 127.71 ns | 113.21 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-26T02:57:20.474Z*
