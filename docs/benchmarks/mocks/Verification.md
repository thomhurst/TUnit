# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 614.37 ns     | 1.663 ns   | 1.556 ns   | 3008 B    |
| Imposter        | 553.42 ns     | 1.411 ns   | 1.178 ns   | 4688 B    |
| Mockolate       | 365.87 ns     | 1.126 ns   | 0.998 ns   | 2128 B    |
| Moq             | 149,272.81 ns | 462.371 ns | 432.502 ns | 24561 B   |
| NSubstitute     | 5,509.19 ns   | 62.640 ns  | 55.528 ns  | 10064 B   |
| FakeItEasy      | 4,515.89 ns   | 37.217 ns  | 34.812 ns  | 10717 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev    | Allocated |
| --------------- | ------------ | ---------- | --------- | --------- |
| **TUnit.Mocks** | 43.52 ns     | 0.156 ns   | 0.146 ns  | 320 B     |
| Imposter        | 259.31 ns    | 0.536 ns   | 0.447 ns  | 2400 B    |
| Mockolate       | 195.37 ns    | 0.578 ns   | 0.541 ns  | 1144 B    |
| Moq             | 37,746.59 ns | 101.780 ns | 90.225 ns | 6922 B    |
| NSubstitute     | 3,058.16 ns  | 14.247 ns  | 13.327 ns | 7088 B    |
| FakeItEasy      | 2,146.12 ns  | 26.146 ns  | 21.833 ns | 5209 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 1,063.01 ns   | 3.271 ns   | 3.059 ns   | 4472 B    |
| Imposter        | 1,374.61 ns   | 22.468 ns  | 21.016 ns  | 11192 B   |
| Mockolate       | 886.51 ns     | 2.400 ns   | 2.245 ns   | 5240 B    |
| Moq             | 185,762.85 ns | 752.307 ns | 666.901 ns | 34584 B   |
| NSubstitute     | 9,474.16 ns   | 184.560 ns | 172.638 ns | 16761 B   |
| FakeItEasy      | 7,937.40 ns   | 136.195 ns | 127.397 ns | 19238 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-16T02:32:43.042Z*
