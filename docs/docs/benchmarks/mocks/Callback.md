---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 526.1 ns | 3.14 ns | 2.94 ns | 3.11 KB |
| Imposter | 357.7 ns | 1.20 ns | 1.12 ns | 2.66 KB |
| Mockolate | 266.7 ns | 1.48 ns | 1.31 ns | 1.8 KB |
| Moq | 105,106.3 ns | 512.04 ns | 399.77 ns | 13.29 KB |
| NSubstitute | 3,433.5 ns | 28.25 ns | 23.59 ns | 7.85 KB |
| FakeItEasy | 3,735.9 ns | 29.36 ns | 27.46 ns | 7.44 KB |

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
  title "Callback Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 126128
  bar [526.1, 357.7, 266.7, 105106.3, 3433.5, 3735.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 599.9 ns | 3.08 ns | 2.88 ns | 3.2 KB |
| Imposter | 414.0 ns | 0.88 ns | 0.69 ns | 2.82 KB |
| Mockolate | 304.8 ns | 0.71 ns | 0.63 ns | 1.84 KB |
| Moq | 112,273.8 ns | 585.37 ns | 488.81 ns | 13.76 KB |
| NSubstitute | 3,942.5 ns | 25.63 ns | 22.72 ns | 8.41 KB |
| FakeItEasy | 4,349.1 ns | 16.12 ns | 13.46 ns | 9.26 KB |

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
  title "Callback (with args) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 134729
  bar [599.9, 414, 304.8, 112273.8, 3942.5, 4349.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-17T02:43:20.076Z*
