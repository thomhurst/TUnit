# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 441.6 ns     | 4.44 ns     | 4.16 ns     | 2.34 KB   |
| Imposter        | 625.7 ns     | 11.65 ns    | 11.45 ns    | 6.12 KB   |
| Mockolate       | 254.0 ns     | 2.48 ns     | 2.20 ns     | 1.41 KB   |
| Moq             | 239,940.3 ns | 1,208.53 ns | 1,009.17 ns | 28.56 KB  |
| NSubstitute     | 4,580.9 ns   | 58.18 ns    | 54.43 ns    | 9.01 KB   |
| FakeItEasy      | 5,714.1 ns   | 112.96 ns   | 110.94 ns   | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 626.8 ns    | 4.57 ns   | 3.81 ns   | 3.15 KB   |
| Imposter        | 1,047.5 ns  | 19.61 ns  | 17.39 ns  | 10.59 KB  |
| Mockolate       | 444.8 ns    | 5.77 ns   | 5.12 ns   | 2.35 KB   |
| Moq             | 69,352.2 ns | 247.43 ns | 231.45 ns | 16.35 KB  |
| NSubstitute     | 8,940.9 ns  | 153.79 ns | 128.42 ns | 20.31 KB  |
| FakeItEasy      | 5,413.7 ns  | 105.86 ns | 144.91 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-27T02:37:02.890Z*
