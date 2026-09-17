# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 277.5 ns   | 106.77 ns | 5.85 ns  | 128 B     |
| Imposter        | 299.3 ns   | 53.03 ns  | 2.91 ns  | 168 B     |
| Mockolate       | 115.1 ns   | 104.90 ns | 5.75 ns  | 84 B      |
| Moq             | 843.5 ns   | 254.96 ns | 13.98 ns | 376 B     |
| NSubstitute     | 790.7 ns   | 211.53 ns | 11.59 ns | 360 B     |
| FakeItEasy      | 1,822.5 ns | 313.92 ns | 17.21 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 166.3 ns   | 76.35 ns  | 4.19 ns  | 96 B      |
| Imposter        | 299.8 ns   | 102.33 ns | 5.61 ns  | 168 B     |
| Mockolate       | 103.9 ns   | 62.72 ns  | 3.44 ns  | 60 B      |
| Moq             | 553.1 ns   | 112.75 ns | 6.18 ns  | 296 B     |
| NSubstitute     | 620.7 ns   | 196.12 ns | 10.75 ns | 272 B     |
| FakeItEasy      | 1,632.0 ns | 497.93 ns | 27.29 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean         | Error         | StdDev      | Allocated |
| --------------- | ------------ | ------------- | ----------- | --------- |
| **TUnit.Mocks** | 27,485.1 ns  | 12,573.78 ns  | 689.21 ns   | 12736 B   |
| Imposter        | 29,372.1 ns  | 10,100.67 ns  | 553.65 ns   | 16800 B   |
| Mockolate       | 11,747.4 ns  | 4,274.57 ns   | 234.30 ns   | 8400 B    |
| Moq             | 83,572.4 ns  | 17,406.89 ns  | 954.13 ns   | 37600 B   |
| NSubstitute     | 71,612.5 ns  | 11,396.06 ns  | 624.66 ns   | 30848 B   |
| FakeItEasy      | 187,364.3 ns | 102,372.25 ns | 5,611.37 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-17T02:33:27.459Z*
