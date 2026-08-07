---
title: "Mock Benchmark: Invocation"
description: "Calling methods on mock objects — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 4
---

# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Calling methods on mock objects:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 220.83 ns | 66.07 ns | 3.622 ns | 128 B |
| Imposter | 237.99 ns | 24.30 ns | 1.332 ns | 168 B |
| Mockolate | 88.62 ns | 27.21 ns | 1.492 ns | 84 B |
| Moq | 635.91 ns | 23.46 ns | 1.286 ns | 376 B |
| NSubstitute | 590.47 ns | 211.78 ns | 11.608 ns | 304 B |
| FakeItEasy | 1,425.06 ns | 200.79 ns | 11.006 ns | 944 B |

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
  y-axis "Time (ns)" 0 --> 1711
  bar [220.83, 237.99, 88.62, 635.91, 590.47, 1425.06]
```

---

### String

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 137.29 ns | 54.98 ns | 3.014 ns | 96 B |
| Imposter | 237.61 ns | 14.43 ns | 0.791 ns | 168 B |
| Mockolate | 79.34 ns | 16.85 ns | 0.924 ns | 60 B |
| Moq | 429.11 ns | 89.28 ns | 4.894 ns | 296 B |
| NSubstitute | 484.86 ns | 138.40 ns | 7.586 ns | 272 B |
| FakeItEasy | 1,276.55 ns | 110.53 ns | 6.059 ns | 776 B |

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
  y-axis "Time (ns)" 0 --> 1532
  bar [137.29, 237.61, 79.34, 429.11, 484.86, 1276.55]
```

---

### 100 calls

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 21,444.91 ns | 9,947.82 ns | 545.274 ns | 12736 B |
| Imposter | 23,364.56 ns | 1,321.08 ns | 72.413 ns | 16800 B |
| Mockolate | 9,023.10 ns | 1,312.45 ns | 71.940 ns | 8400 B |
| Moq | 61,562.38 ns | 10,153.15 ns | 556.529 ns | 37600 B |
| NSubstitute | 63,622.43 ns | 11,487.06 ns | 629.645 ns | 36448 B |
| FakeItEasy | 143,080.25 ns | 65,765.47 ns | 3,604.827 ns | 94400 B |

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
  y-axis "Time (ns)" 0 --> 171697
  bar [21444.91, 23364.56, 9023.1, 61562.38, 63622.43, 143080.25]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-07T03:18:12.757Z*
