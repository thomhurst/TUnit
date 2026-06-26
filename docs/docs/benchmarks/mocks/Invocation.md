---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 276.69 ns | 105.91 ns | 5.806 ns | 128 B |
| Imposter | 297.32 ns | 90.84 ns | 4.979 ns | 168 B |
| Mockolate | 121.69 ns | 37.06 ns | 2.031 ns | 84 B |
| Moq | 850.31 ns | 203.77 ns | 11.170 ns | 376 B |
| NSubstitute | 719.46 ns | 210.89 ns | 11.559 ns | 304 B |
| FakeItEasy | 1,719.66 ns | 82.65 ns | 4.530 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 2064
  bar [276.69, 297.32, 121.69, 850.31, 719.46, 1719.66]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 165.24 ns | 76.14 ns | 4.174 ns | 96 B |
| Imposter | 296.39 ns | 90.86 ns | 4.981 ns | 168 B |
| Mockolate | 97.93 ns | 53.04 ns | 2.907 ns | 60 B |
| Moq | 534.88 ns | 291.61 ns | 15.984 ns | 296 B |
| NSubstitute | 593.67 ns | 148.62 ns | 8.147 ns | 272 B |
| FakeItEasy | 1,513.98 ns | 613.91 ns | 33.650 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1817
  bar [165.24, 296.39, 97.93, 534.88, 593.67, 1513.98]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 26,826.63 ns | 11,155.64 ns | 611.478 ns | 12736 B |
| Imposter | 29,511.61 ns | 9,574.76 ns | 524.825 ns | 16800 B |
| Mockolate | 11,657.19 ns | 7,778.52 ns | 426.367 ns | 8400 B |
| Moq | 84,109.90 ns | 6,896.02 ns | 377.994 ns | 37600 B |
| NSubstitute | 81,507.37 ns | 43,734.10 ns | 2,397.213 ns | 36448 B |
| FakeItEasy | 189,149.81 ns | 17,620.42 ns | 965.835 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 226980
  bar [26826.63, 29511.61, 11657.19, 84109.9, 81507.37, 189149.81]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-26T03:28:53.126Z*
