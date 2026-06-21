---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 220.25 ns | 43.05 ns | 2.360 ns | 128 B |
| Imposter | 240.28 ns | 35.59 ns | 1.951 ns | 168 B |
| Mockolate | 108.57 ns | 69.86 ns | 3.829 ns | 84 B |
| Moq | 684.51 ns | 183.82 ns | 10.076 ns | 376 B |
| NSubstitute | 658.74 ns | 80.58 ns | 4.417 ns | 360 B |
| FakeItEasy | 1,451.31 ns | 235.27 ns | 12.896 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1742
  bar [220.25, 240.28, 108.57, 684.51, 658.74, 1451.31]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 135.91 ns | 52.49 ns | 2.877 ns | 96 B |
| Imposter | 237.77 ns | 29.65 ns | 1.625 ns | 168 B |
| Mockolate | 88.82 ns | 16.26 ns | 0.891 ns | 60 B |
| Moq | 444.19 ns | 68.82 ns | 3.772 ns | 296 B |
| NSubstitute | 538.93 ns | 141.00 ns | 7.729 ns | 328 B |
| FakeItEasy | 1,370.10 ns | 188.02 ns | 10.306 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1645
  bar [135.91, 237.77, 88.82, 444.19, 538.93, 1370.1]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 21,590.50 ns | 5,137.39 ns | 281.598 ns | 12736 B |
| Imposter | 23,297.52 ns | 2,006.23 ns | 109.968 ns | 16800 B |
| Mockolate | 8,886.94 ns | 950.61 ns | 52.106 ns | 8400 B |
| Moq | 62,485.05 ns | 15,067.14 ns | 825.881 ns | 37600 B |
| NSubstitute | 60,574.60 ns | 35,035.14 ns | 1,920.395 ns | 30848 B |
| FakeItEasy | 144,792.15 ns | 43,425.40 ns | 2,380.293 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 173751
  bar [21590.5, 23297.52, 8886.94, 62485.05, 60574.6, 144792.15]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-21T03:36:43.702Z*
