---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 269.68 ns | 164.45 ns | 9.014 ns | 128 B |
| Imposter | 298.26 ns | 89.19 ns | 4.889 ns | 168 B |
| Mockolate | 111.08 ns | 21.79 ns | 1.194 ns | 84 B |
| Moq | 828.17 ns | 174.33 ns | 9.556 ns | 376 B |
| NSubstitute | 720.82 ns | 305.44 ns | 16.742 ns | 304 B |
| FakeItEasy | 1,760.65 ns | 1,037.07 ns | 56.845 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2113
  bar [269.68, 298.26, 111.08, 828.17, 720.82, 1760.65]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 164.69 ns | 72.02 ns | 3.947 ns | 96 B |
| Imposter | 286.33 ns | 58.42 ns | 3.202 ns | 168 B |
| Mockolate | 92.44 ns | 24.81 ns | 1.360 ns | 60 B |
| Moq | 520.79 ns | 74.51 ns | 4.084 ns | 296 B |
| NSubstitute | 604.30 ns | 149.59 ns | 8.200 ns | 272 B |
| FakeItEasy | 1,487.39 ns | 81.54 ns | 4.469 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1785
  bar [164.69, 286.33, 92.44, 520.79, 604.3, 1487.39]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,708.18 ns | 10,087.12 ns | 552.909 ns | 12736 B |
| Imposter | 28,630.58 ns | 7,733.92 ns | 423.922 ns | 16800 B |
| Mockolate | 10,046.12 ns | 5,310.23 ns | 291.071 ns | 8400 B |
| Moq | 78,248.39 ns | 18,236.31 ns | 999.593 ns | 37600 B |
| NSubstitute | 71,860.34 ns | 15,718.68 ns | 861.594 ns | 30848 B |
| FakeItEasy | 170,733.67 ns | 41,628.58 ns | 2,281.803 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 204881
  bar [26708.18, 28630.58, 10046.12, 78248.39, 71860.34, 170733.67]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-12T03:10:08.627Z*
