---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.32 ns | 89.80 ns | 4.922 ns | 128 B |
| Imposter | 304.35 ns | 42.48 ns | 2.329 ns | 168 B |
| Mockolate | 128.24 ns | 23.60 ns | 1.294 ns | 84 B |
| Moq | 868.43 ns | 166.31 ns | 9.116 ns | 376 B |
| NSubstitute | 736.11 ns | 229.78 ns | 12.595 ns | 304 B |
| FakeItEasy | 1,815.76 ns | 1,519.30 ns | 83.278 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2179
  bar [281.32, 304.35, 128.24, 868.43, 736.11, 1815.76]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.79 ns | 69.76 ns | 3.824 ns | 96 B |
| Imposter | 291.43 ns | 120.48 ns | 6.604 ns | 168 B |
| Mockolate | 91.73 ns | 39.51 ns | 2.165 ns | 60 B |
| Moq | 532.28 ns | 108.96 ns | 5.972 ns | 296 B |
| NSubstitute | 587.14 ns | 290.07 ns | 15.900 ns | 272 B |
| FakeItEasy | 1,567.77 ns | 766.96 ns | 42.039 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1882
  bar [165.79, 291.43, 91.73, 532.28, 587.14, 1567.77]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,833.66 ns | 9,657.89 ns | 529.382 ns | 12736 B |
| Imposter | 28,304.42 ns | 3,294.21 ns | 180.567 ns | 16800 B |
| Mockolate | 10,050.81 ns | 2,065.04 ns | 113.192 ns | 8400 B |
| Moq | 77,403.93 ns | 17,324.64 ns | 949.622 ns | 37600 B |
| NSubstitute | 72,020.24 ns | 19,981.94 ns | 1,095.277 ns | 30848 B |
| FakeItEasy | 188,386.12 ns | 76,624.29 ns | 4,200.036 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 226064
  bar [26833.66, 28304.42, 10050.81, 77403.93, 72020.24, 188386.12]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-29T03:30:39.957Z*
