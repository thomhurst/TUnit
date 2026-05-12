---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 696.72 ns | 2.927 ns | 2.738 ns | 2864 B |
| Imposter | 708.68 ns | 11.942 ns | 11.170 ns | 4688 B |
| Mockolate | 407.39 ns | 2.010 ns | 1.880 ns | 2240 B |
| Moq | 237,297.90 ns | 867.785 ns | 724.640 ns | 24324 B |
| NSubstitute | 6,062.62 ns | 49.855 ns | 44.195 ns | 10176 B |
| FakeItEasy | 6,592.55 ns | 80.650 ns | 75.440 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 284758
  bar [696.72, 708.68, 407.39, 237297.9, 6062.62, 6592.55]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.81 ns | 0.393 ns | 0.368 ns | 304 B |
| Imposter | 330.18 ns | 1.560 ns | 1.459 ns | 2400 B |
| Mockolate | 253.07 ns | 0.758 ns | 0.672 ns | 1240 B |
| Moq | 60,794.75 ns | 468.270 ns | 415.109 ns | 6925 B |
| NSubstitute | 3,404.64 ns | 39.053 ns | 36.530 ns | 7088 B |
| FakeItEasy | 3,226.32 ns | 26.528 ns | 22.152 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 72954
  bar [53.81, 330.18, 253.07, 60794.75, 3404.64, 3226.32]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,200.53 ns | 6.093 ns | 5.402 ns | 4176 B |
| Imposter | 1,836.41 ns | 20.095 ns | 17.813 ns | 11192 B |
| Mockolate | 1,266.53 ns | 17.545 ns | 16.412 ns | 5376 B |
| Moq | 351,877.55 ns | 3,044.033 ns | 2,847.390 ns | 34699 B |
| NSubstitute | 10,598.54 ns | 124.590 ns | 116.542 ns | 16889 B |
| FakeItEasy | 12,434.19 ns | 120.322 ns | 106.662 ns | 19424 B |

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
  y-axis "Time (ns)" 0 --> 422254
  bar [1200.53, 1836.41, 1266.53, 351877.55, 10598.54, 12434.19]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-12T03:27:02.666Z*
