# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 576.5 ns     | 9.98 ns     | 9.33 ns     | 2.34 KB   |
| Imposter        | 695.1 ns     | 7.88 ns     | 7.73 ns     | 6.12 KB   |
| Mockolate       | 324.3 ns     | 4.96 ns     | 6.44 ns     | 1.41 KB   |
| Moq             | 191,348.4 ns | 2,163.96 ns | 1,918.30 ns | 28.46 KB  |
| NSubstitute     | 5,669.8 ns   | 99.78 ns    | 88.45 ns    | 9.06 KB   |
| FakeItEasy      | 5,613.9 ns   | 57.22 ns    | 53.52 ns    | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 806.6 ns    | 15.87 ns  | 25.16 ns  | 3.15 KB   |
| Imposter        | 1,218.2 ns  | 23.62 ns  | 33.11 ns  | 10.59 KB  |
| Mockolate       | 543.2 ns    | 10.76 ns  | 18.27 ns  | 2.35 KB   |
| Moq             | 52,879.2 ns | 808.72 ns | 756.47 ns | 16.63 KB  |
| NSubstitute     | 9,839.9 ns  | 120.18 ns | 112.42 ns | 20.31 KB  |
| FakeItEasy      | 5,638.6 ns  | 109.30 ns | 138.22 ns | 11.81 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-24T02:33:35.117Z*
