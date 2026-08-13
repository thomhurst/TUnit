---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 271.27 ns | 103.81 ns | 5.690 ns | 128 B |
| Imposter | 291.24 ns | 27.92 ns | 1.531 ns | 168 B |
| Mockolate | 103.21 ns | 16.15 ns | 0.885 ns | 84 B |
| Moq | 777.36 ns | 177.21 ns | 9.713 ns | 376 B |
| NSubstitute | 704.88 ns | 183.12 ns | 10.037 ns | 304 B |
| FakeItEasy | 1,697.43 ns | 421.18 ns | 23.087 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2037
  bar [271.27, 291.24, 103.21, 777.36, 704.88, 1697.43]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.60 ns | 70.45 ns | 3.861 ns | 96 B |
| Imposter | 289.32 ns | 74.42 ns | 4.079 ns | 168 B |
| Mockolate | 94.23 ns | 48.84 ns | 2.677 ns | 60 B |
| Moq | 539.45 ns | 143.86 ns | 7.886 ns | 296 B |
| NSubstitute | 653.06 ns | 186.83 ns | 10.241 ns | 328 B |
| FakeItEasy | 1,518.09 ns | 234.88 ns | 12.875 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1822
  bar [164.6, 289.32, 94.23, 539.45, 653.06, 1518.09]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27,059.02 ns | 9,802.42 ns | 537.304 ns | 12736 B |
| Imposter | 28,972.46 ns | 8,516.92 ns | 466.841 ns | 16800 B |
| Mockolate | 10,218.31 ns | 2,665.59 ns | 146.110 ns | 8400 B |
| Moq | 78,298.38 ns | 8,722.90 ns | 478.131 ns | 37600 B |
| NSubstitute | 70,841.76 ns | 8,724.99 ns | 478.246 ns | 30848 B |
| FakeItEasy | 170,617.91 ns | 71,063.17 ns | 3,895.212 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 204742
  bar [27059.02, 28972.46, 10218.31, 78298.38, 70841.76, 170617.91]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-13T03:11:34.997Z*
