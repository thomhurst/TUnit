---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 897.91 ns | 14.829 ns | 13.871 ns | 3080 B |
| Imposter | 781.59 ns | 15.263 ns | 24.646 ns | 4688 B |
| Mockolate | 1,003.39 ns | 19.597 ns | 18.331 ns | 3152 B |
| Moq | 256,106.68 ns | 1,979.893 ns | 1,851.994 ns | 24306 B |
| NSubstitute | 6,446.08 ns | 41.907 ns | 34.994 ns | 10064 B |
| FakeItEasy | 7,042.92 ns | 54.032 ns | 50.542 ns | 10731 B |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 307329
  bar [897.91, 781.59, 1003.39, 256106.68, 6446.08, 7042.92]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 68.40 ns | 1.398 ns | 1.373 ns | 328 B |
| Imposter | 342.32 ns | 6.738 ns | 7.489 ns | 2400 B |
| Mockolate | 230.98 ns | 2.824 ns | 2.641 ns | 952 B |
| Moq | 65,754.24 ns | 372.187 ns | 348.144 ns | 7037 B |
| NSubstitute | 3,571.90 ns | 46.514 ns | 43.509 ns | 7088 B |
| FakeItEasy | 3,229.37 ns | 30.948 ns | 28.949 ns | 5217 B |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 78906
  bar [68.4, 342.32, 230.98, 65754.24, 3571.9, 3229.37]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,500.67 ns | 21.519 ns | 20.129 ns | 4608 B |
| Imposter | 1,873.01 ns | 29.880 ns | 27.950 ns | 11192 B |
| Mockolate | 1,985.80 ns | 17.840 ns | 16.688 ns | 5496 B |
| Moq | 357,897.98 ns | 2,386.890 ns | 2,232.699 ns | 35085 B |
| NSubstitute | 10,924.59 ns | 106.904 ns | 99.998 ns | 16761 B |
| FakeItEasy | 12,187.52 ns | 73.921 ns | 69.145 ns | 19239 B |

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 429478
  bar [1500.67, 1873.01, 1985.8, 357897.98, 10924.59, 12187.52]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-12T03:28:39.462Z*
