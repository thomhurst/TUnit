---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 273.66 ns | 87.46 ns | 4.794 ns | 128 B |
| Imposter | 294.56 ns | 55.72 ns | 3.054 ns | 168 B |
| Mockolate | 102.81 ns | 20.57 ns | 1.128 ns | 84 B |
| Moq | 787.82 ns | 63.52 ns | 3.482 ns | 376 B |
| NSubstitute | 721.47 ns | 94.59 ns | 5.185 ns | 304 B |
| FakeItEasy | 1,704.29 ns | 430.16 ns | 23.579 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2046
  bar [273.66, 294.56, 102.81, 787.82, 721.47, 1704.29]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 163.54 ns | 72.51 ns | 3.974 ns | 96 B |
| Imposter | 294.60 ns | 80.66 ns | 4.421 ns | 168 B |
| Mockolate | 91.78 ns | 42.15 ns | 2.310 ns | 60 B |
| Moq | 525.73 ns | 127.73 ns | 7.001 ns | 296 B |
| NSubstitute | 603.15 ns | 171.72 ns | 9.412 ns | 272 B |
| FakeItEasy | 1,501.74 ns | 277.55 ns | 15.214 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1803
  bar [163.54, 294.6, 91.78, 525.73, 603.15, 1501.74]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,706.83 ns | 8,460.54 ns | 463.751 ns | 12736 B |
| Imposter | 28,896.40 ns | 10,152.47 ns | 556.491 ns | 16800 B |
| Mockolate | 9,949.26 ns | 4,182.48 ns | 229.256 ns | 8400 B |
| Moq | 79,311.23 ns | 2,220.44 ns | 121.710 ns | 37600 B |
| NSubstitute | 69,908.29 ns | 8,469.49 ns | 464.241 ns | 30848 B |
| FakeItEasy | 173,665.99 ns | 13,715.05 ns | 751.768 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 208400
  bar [26706.83, 28896.4, 9949.26, 79311.23, 69908.29, 173665.99]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-18T02:32:22.133Z*
