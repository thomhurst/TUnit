# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 479.9 ns     | 4.23 ns     | 3.95 ns     | 2.38 KB   |
| Imposter        | 286.7 ns     | 2.28 ns     | 2.02 ns     | 1.33 KB   |
| Mockolate       | 279.6 ns     | 3.35 ns     | 2.80 ns     | 1.39 KB   |
| Moq             | 464,100.7 ns | 2,113.20 ns | 1,976.69 ns | 28.52 KB  |
| NSubstitute     | 5,980.5 ns   | 45.47 ns    | 42.53 ns    | 8.97 KB   |
| FakeItEasy      | 7,578.4 ns   | 87.30 ns    | 81.66 ns    | 10.14 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 727.1 ns     | 9.73 ns   | 9.10 ns   | 3.2 KB    |
| Imposter        | 528.1 ns     | 3.30 ns   | 2.57 ns   | 2.48 KB   |
| Mockolate       | 493.4 ns     | 4.49 ns   | 3.98 ns   | 2.31 KB   |
| Moq             | 115,093.5 ns | 872.28 ns | 815.93 ns | 16.03 KB  |
| NSubstitute     | 11,817.8 ns  | 42.51 ns  | 39.77 ns  | 20.17 KB  |
| FakeItEasy      | 6,943.5 ns   | 32.65 ns  | 28.95 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-17T02:23:59.645Z*
