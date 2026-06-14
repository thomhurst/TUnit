---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 286.1 ns | 47.63 ns | 2.61 ns | 128 B |
| Imposter | 312.8 ns | 125.21 ns | 6.86 ns | 168 B |
| Mockolate | 146.2 ns | 65.02 ns | 3.56 ns | 84 B |
| Moq | 890.3 ns | 85.09 ns | 4.66 ns | 376 B |
| NSubstitute | 790.1 ns | 201.16 ns | 11.03 ns | 304 B |
| FakeItEasy | 2,023.3 ns | 751.50 ns | 41.19 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2428
  bar [286.1, 312.8, 146.2, 890.3, 790.1, 2023.3]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 176.0 ns | 70.16 ns | 3.85 ns | 96 B |
| Imposter | 317.1 ns | 105.63 ns | 5.79 ns | 168 B |
| Mockolate | 127.2 ns | 195.31 ns | 10.71 ns | 60 B |
| Moq | 624.4 ns | 205.35 ns | 11.26 ns | 296 B |
| NSubstitute | 696.9 ns | 221.12 ns | 12.12 ns | 272 B |
| FakeItEasy | 1,844.7 ns | 253.25 ns | 13.88 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 2214
  bar [176, 317.1, 127.2, 624.4, 696.9, 1844.7]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,340.7 ns | 15,973.75 ns | 875.58 ns | 12736 B |
| Imposter | 31,171.9 ns | 10,812.47 ns | 592.67 ns | 16800 B |
| Mockolate | 15,026.1 ns | 10,837.89 ns | 594.06 ns | 8400 B |
| Moq | 91,382.4 ns | 21,024.77 ns | 1,152.44 ns | 37600 B |
| NSubstitute | 80,641.2 ns | 32,824.22 ns | 1,799.21 ns | 30848 B |
| FakeItEasy | 206,796.1 ns | 87,347.42 ns | 4,787.81 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 248156
  bar [28340.7, 31171.9, 15026.1, 91382.4, 80641.2, 206796.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-14T03:35:08.044Z*
