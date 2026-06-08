---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 272.41 ns | 101.54 ns | 5.566 ns | 128 B |
| Imposter | 316.34 ns | 132.88 ns | 7.284 ns | 168 B |
| Mockolate | 106.63 ns | 118.82 ns | 6.513 ns | 84 B |
| Moq | 804.99 ns | 267.02 ns | 14.636 ns | 376 B |
| NSubstitute | 738.91 ns | 162.46 ns | 8.905 ns | 304 B |
| FakeItEasy | 1,749.23 ns | 155.89 ns | 8.545 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2100
  bar [272.41, 316.34, 106.63, 804.99, 738.91, 1749.23]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.74 ns | 89.03 ns | 4.880 ns | 96 B |
| Imposter | 289.58 ns | 51.75 ns | 2.837 ns | 168 B |
| Mockolate | 98.14 ns | 17.94 ns | 0.983 ns | 60 B |
| Moq | 531.02 ns | 76.42 ns | 4.189 ns | 296 B |
| NSubstitute | 601.77 ns | 309.38 ns | 16.958 ns | 272 B |
| FakeItEasy | 1,470.23 ns | 236.64 ns | 12.971 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1765
  bar [165.74, 289.58, 98.14, 531.02, 601.77, 1470.23]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,671.33 ns | 11,333.23 ns | 621.212 ns | 12736 B |
| Imposter | 28,938.89 ns | 2,263.55 ns | 124.073 ns | 16800 B |
| Mockolate | 10,042.60 ns | 1,366.49 ns | 74.902 ns | 8400 B |
| Moq | 78,976.75 ns | 14,347.00 ns | 786.408 ns | 37600 B |
| NSubstitute | 72,723.10 ns | 32,892.55 ns | 1,802.952 ns | 30848 B |
| FakeItEasy | 168,862.28 ns | 43,865.98 ns | 2,404.443 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 202635
  bar [26671.33, 28938.89, 10042.6, 78976.75, 72723.1, 168862.28]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-08T03:30:49.435Z*
