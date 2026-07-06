---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 284.8 ns | 111.57 ns | 6.12 ns | 128 B |
| Imposter | 310.9 ns | 26.71 ns | 1.46 ns | 168 B |
| Mockolate | 131.1 ns | 10.83 ns | 0.59 ns | 84 B |
| Moq | 870.8 ns | 157.65 ns | 8.64 ns | 376 B |
| NSubstitute | 792.7 ns | 374.28 ns | 20.52 ns | 304 B |
| FakeItEasy | 1,943.6 ns | 437.25 ns | 23.97 ns | 944 B |

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
  title "Invocation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2333
  bar [284.8, 310.9, 131.1, 870.8, 792.7, 1943.6]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 170.7 ns | 68.79 ns | 3.77 ns | 96 B |
| Imposter | 299.6 ns | 63.81 ns | 3.50 ns | 168 B |
| Mockolate | 106.6 ns | 21.89 ns | 1.20 ns | 60 B |
| Moq | 581.5 ns | 61.56 ns | 3.37 ns | 296 B |
| NSubstitute | 671.7 ns | 443.05 ns | 24.29 ns | 272 B |
| FakeItEasy | 1,664.6 ns | 99.36 ns | 5.45 ns | 776 B |

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
  title "Invocation (String) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 1998
  bar [170.7, 299.6, 106.6, 581.5, 671.7, 1664.6]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,671.7 ns | 9,945.51 ns | 545.15 ns | 12736 B |
| Imposter | 30,246.9 ns | 11,228.71 ns | 615.48 ns | 16800 B |
| Mockolate | 12,012.9 ns | 3,112.91 ns | 170.63 ns | 8400 B |
| Moq | 84,421.8 ns | 35,025.90 ns | 1,919.89 ns | 37600 B |
| NSubstitute | 72,612.7 ns | 11,188.09 ns | 613.26 ns | 30848 B |
| FakeItEasy | 195,264.4 ns | 46,774.19 ns | 2,563.85 ns | 94400 B |

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
  title "Invocation (100 calls) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 234318
  bar [27671.7, 30246.9, 12012.9, 84421.8, 72612.7, 195264.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-06T03:43:04.080Z*
