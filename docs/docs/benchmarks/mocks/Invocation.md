---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 281.83 ns | 75.53 ns | 4.140 ns | 128 B |
| Imposter | 307.95 ns | 100.45 ns | 5.506 ns | 168 B |
| Mockolate | 111.46 ns | 31.67 ns | 1.736 ns | 84 B |
| Moq | 825.03 ns | 106.02 ns | 5.811 ns | 376 B |
| NSubstitute | 752.19 ns | 211.22 ns | 11.578 ns | 304 B |
| FakeItEasy | 1,854.86 ns | 318.60 ns | 17.464 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2226
  bar [281.83, 307.95, 111.46, 825.03, 752.19, 1854.86]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 174.79 ns | 59.78 ns | 3.277 ns | 96 B |
| Imposter | 306.81 ns | 98.26 ns | 5.386 ns | 168 B |
| Mockolate | 98.06 ns | 30.54 ns | 1.674 ns | 60 B |
| Moq | 558.28 ns | 135.77 ns | 7.442 ns | 296 B |
| NSubstitute | 632.93 ns | 190.22 ns | 10.426 ns | 272 B |
| FakeItEasy | 1,636.07 ns | 134.46 ns | 7.370 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1964
  bar [174.79, 306.81, 98.06, 558.28, 632.93, 1636.07]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 28,386.83 ns | 8,563.62 ns | 469.401 ns | 12736 B |
| Imposter | 30,696.62 ns | 6,377.16 ns | 349.554 ns | 16800 B |
| Mockolate | 11,010.20 ns | 2,582.36 ns | 141.548 ns | 8400 B |
| Moq | 81,516.95 ns | 8,906.73 ns | 488.208 ns | 37600 B |
| NSubstitute | 74,277.06 ns | 11,856.75 ns | 649.908 ns | 30848 B |
| FakeItEasy | 184,742.60 ns | 50,928.56 ns | 2,791.566 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 221692
  bar [28386.83, 30696.62, 11010.2, 81516.95, 74277.06, 184742.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-11T02:43:35.245Z*
