# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 571.6 ns     | 11.25 ns    | 13.40 ns    | 2.34 KB   |
| Imposter        | 888.6 ns     | 17.86 ns    | 50.38 ns    | 6.12 KB   |
| Mockolate       | 324.5 ns     | 5.92 ns     | 5.53 ns     | 1.41 KB   |
| Moq             | 432,633.6 ns | 2,361.65 ns | 2,093.54 ns | 28.52 KB  |
| NSubstitute     | 6,373.6 ns   | 91.15 ns    | 85.26 ns    | 9.01 KB   |
| FakeItEasy      | 8,280.3 ns   | 159.81 ns   | 149.49 ns   | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 830.1 ns     | 16.53 ns    | 22.62 ns    | 3.15 KB   |
| Imposter        | 1,475.0 ns   | 27.91 ns    | 67.41 ns    | 10.59 KB  |
| Mockolate       | 551.0 ns     | 10.58 ns    | 12.99 ns    | 2.35 KB   |
| Moq             | 117,217.6 ns | 1,424.01 ns | 1,332.02 ns | 16.53 KB  |
| NSubstitute     | 13,080.0 ns  | 127.95 ns   | 119.69 ns   | 20.31 KB  |
| FakeItEasy      | 8,082.4 ns   | 156.85 ns   | 146.72 ns   | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-20T02:32:50.293Z*
