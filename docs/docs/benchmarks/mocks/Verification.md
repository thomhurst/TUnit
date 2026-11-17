---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 642.56 ns | 10.228 ns | 12.935 ns | 3040 B |
| Imposter | 245.61 ns | 4.884 ns | 8.681 ns | 1096 B |
| Mockolate | 354.26 ns | 5.925 ns | 6.085 ns | 1824 B |
| Moq | 140,787.42 ns | 756.447 ns | 707.581 ns | 24314 B |
| NSubstitute | 5,156.06 ns | 79.997 ns | 74.829 ns | 10024 B |
| FakeItEasy | 4,143.91 ns | 53.651 ns | 47.560 ns | 10501 B |

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
  y-axis "Time (ns)" 0 --> 168945
  bar [642.56, 245.61, 354.26, 140787.42, 5156.06, 4143.91]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.55 ns | 0.895 ns | 1.066 ns | 336 B |
| Imposter | 119.62 ns | 2.390 ns | 4.489 ns | 616 B |
| Mockolate | 199.28 ns | 3.957 ns | 4.711 ns | 1040 B |
| Moq | 34,044.10 ns | 190.048 ns | 168.473 ns | 6914 B |
| NSubstitute | 2,506.69 ns | 26.173 ns | 24.482 ns | 7056 B |
| FakeItEasy | 2,134.31 ns | 14.787 ns | 13.108 ns | 5118 B |

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
  y-axis "Time (ns)" 0 --> 40853
  bar [46.55, 119.62, 199.28, 34044.1, 2506.69, 2134.31]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,031.30 ns | 8.477 ns | 7.079 ns | 4528 B |
| Imposter | 467.11 ns | 3.228 ns | 2.862 ns | 2288 B |
| Mockolate | 696.47 ns | 11.345 ns | 10.612 ns | 3264 B |
| Moq | 180,659.33 ns | 1,238.485 ns | 1,158.480 ns | 34847 B |
| NSubstitute | 8,275.87 ns | 137.386 ns | 121.789 ns | 16633 B |
| FakeItEasy | 7,510.00 ns | 145.183 ns | 198.728 ns | 18716 B |

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
  y-axis "Time (ns)" 0 --> 216792
  bar [1031.3, 467.11, 696.47, 180659.33, 8275.87, 7510]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-17T02:23:59.645Z*
