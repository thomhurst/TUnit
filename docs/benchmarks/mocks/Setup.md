# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 513.9 ns     | 3.07 ns     | 2.57 ns     | 2.34 KB   |
| Imposter        | 826.0 ns     | 8.78 ns     | 8.22 ns     | 6.12 KB   |
| Mockolate       | 305.0 ns     | 3.34 ns     | 3.13 ns     | 1.41 KB   |
| Moq             | 431,926.9 ns | 2,913.99 ns | 2,725.75 ns | 28.63 KB  |
| NSubstitute     | 6,083.8 ns   | 35.96 ns    | 33.63 ns    | 9.01 KB   |
| FakeItEasy      | 8,817.6 ns   | 58.25 ns    | 51.64 ns    | 10.57 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 781.9 ns     | 4.15 ns   | 3.88 ns   | 3.15 KB   |
| Imposter        | 1,371.4 ns   | 7.09 ns   | 6.63 ns   | 10.59 KB  |
| Mockolate       | 526.2 ns     | 4.82 ns   | 4.28 ns   | 2.35 KB   |
| Moq             | 116,398.8 ns | 831.01 ns | 777.33 ns | 16.53 KB  |
| NSubstitute     | 12,664.7 ns  | 249.74 ns | 256.46 ns | 20.5 KB   |
| FakeItEasy      | 8,239.5 ns   | 102.88 ns | 85.91 ns  | 11.82 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-21T02:37:24.392Z*
