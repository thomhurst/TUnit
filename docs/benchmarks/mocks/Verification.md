# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Verifying mock method calls:

| Library         | Mean          | Error      | StdDev     | Allocated |
| --------------- | ------------- | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 567.03 ns     | 11.142 ns  | 10.422 ns  | 3040 B    |
| Imposter        | 214.73 ns     | 2.090 ns   | 1.853 ns   | 1096 B    |
| Mockolate       | 320.95 ns     | 4.640 ns   | 4.340 ns   | 1824 B    |
| Moq             | 135,140.36 ns | 631.220 ns | 590.444 ns | 24314 B   |
| NSubstitute     | 4,999.91 ns   | 95.744 ns  | 106.419 ns | 10024 B   |
| FakeItEasy      | 3,966.32 ns   | 73.074 ns  | 68.353 ns  | 10501 B   |

<!-- -->

***

### Never[​](#never "Direct link to Never")

| Library         | Mean         | Error      | StdDev     | Allocated |
| --------------- | ------------ | ---------- | ---------- | --------- |
| **TUnit.Mocks** | 42.60 ns     | 0.841 ns   | 1.719 ns   | 336 B     |
| Imposter        | 109.34 ns    | 2.176 ns   | 2.673 ns   | 616 B     |
| Mockolate       | 181.87 ns    | 2.742 ns   | 2.564 ns   | 1040 B    |
| Moq             | 33,501.78 ns | 264.746 ns | 247.643 ns | 6914 B    |
| NSubstitute     | 2,319.19 ns  | 15.077 ns  | 14.103 ns  | 7056 B    |
| FakeItEasy      | 1,856.54 ns  | 6.237 ns   | 5.529 ns   | 5119 B    |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 939.21 ns     | 2.763 ns     | 2.584 ns     | 4528 B    |
| Imposter        | 426.38 ns     | 3.664 ns     | 3.427 ns     | 2288 B    |
| Mockolate       | 627.20 ns     | 4.479 ns     | 3.970 ns     | 3264 B    |
| Moq             | 176,049.31 ns | 1,440.891 ns | 1,347.810 ns | 34847 B   |
| NSubstitute     | 8,230.25 ns   | 76.971 ns    | 71.998 ns    | 16633 B   |
| FakeItEasy      | 7,224.92 ns   | 142.350 ns   | 194.850 ns   | 18716 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-18T02:25:26.946Z*
