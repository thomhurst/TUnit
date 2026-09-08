# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 741.0 ns     | 14.69 ns    | 20.59 ns    | 3.11 KB   |
| Imposter        | 533.9 ns     | 8.47 ns     | 7.92 ns     | 2.66 KB   |
| Mockolate       | 373.7 ns     | 5.73 ns     | 5.36 ns     | 1.8 KB    |
| Moq             | 189,494.0 ns | 1,988.29 ns | 1,762.57 ns | 13.14 KB  |
| NSubstitute     | 5,233.0 ns   | 28.92 ns    | 25.64 ns    | 7.85 KB   |
| FakeItEasy      | 5,560.4 ns   | 103.81 ns   | 97.11 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 852.5 ns     | 11.51 ns  | 10.77 ns  | 3.2 KB    |
| Imposter        | 582.3 ns     | 11.52 ns  | 19.24 ns  | 2.82 KB   |
| Mockolate       | 456.7 ns     | 9.07 ns   | 8.91 ns   | 1.84 KB   |
| Moq             | 196,981.5 ns | 676.63 ns | 599.81 ns | 13.73 KB  |
| NSubstitute     | 5,503.5 ns   | 81.81 ns  | 63.87 ns  | 8.41 KB   |
| FakeItEasy      | 6,605.3 ns   | 102.55 ns | 95.93 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-08T02:32:39.573Z*
