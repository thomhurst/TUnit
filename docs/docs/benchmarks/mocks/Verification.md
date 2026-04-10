---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 767.48 ns | 2.768 ns | 2.161 ns | 3080 B |
| Imposter | 677.31 ns | 4.885 ns | 4.570 ns | 4688 B |
| Mockolate | 916.33 ns | 15.450 ns | 14.452 ns | 3152 B |
| Moq | 246,884.95 ns | 2,293.207 ns | 1,914.931 ns | 24324 B |
| NSubstitute | 5,913.41 ns | 112.959 ns | 105.662 ns | 10176 B |
| FakeItEasy | 6,730.82 ns | 40.334 ns | 33.681 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 296262
  bar [767.48, 677.31, 916.33, 246884.95, 5913.41, 6730.82]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 68.01 ns | 1.393 ns | 1.549 ns | 328 B |
| Imposter | 315.59 ns | 2.359 ns | 2.207 ns | 2400 B |
| Mockolate | 226.49 ns | 1.549 ns | 1.373 ns | 952 B |
| Moq | 61,739.48 ns | 466.408 ns | 389.472 ns | 6925 B |
| NSubstitute | 3,663.81 ns | 33.876 ns | 30.030 ns | 7088 B |
| FakeItEasy | 3,348.48 ns | 44.156 ns | 36.872 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74088
  bar [68.01, 315.59, 226.49, 61739.48, 3663.81, 3348.48]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,392.84 ns | 26.218 ns | 24.525 ns | 4608 B |
| Imposter | 1,639.30 ns | 17.109 ns | 16.004 ns | 11192 B |
| Mockolate | 1,799.19 ns | 24.955 ns | 23.343 ns | 5496 B |
| Moq | 342,212.92 ns | 2,307.723 ns | 1,927.054 ns | 34811 B |
| NSubstitute | 10,535.64 ns | 114.829 ns | 101.793 ns | 16762 B |
| FakeItEasy | 11,495.13 ns | 190.052 ns | 177.775 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 410656
  bar [1392.84, 1639.3, 1799.19, 342212.92, 10535.64, 11495.13]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-10T03:23:10.636Z*
