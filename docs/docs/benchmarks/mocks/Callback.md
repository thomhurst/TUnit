---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 758.7 ns | 15.03 ns | 15.44 ns | 3.11 KB |
| Imposter | 514.2 ns | 10.29 ns | 19.58 ns | 2.66 KB |
| Mockolate | 404.1 ns | 8.05 ns | 11.29 ns | 1.8 KB |
| Moq | 188,797.2 ns | 1,578.39 ns | 1,399.20 ns | 13.14 KB |
| NSubstitute | 4,930.0 ns | 36.67 ns | 34.31 ns | 7.93 KB |
| FakeItEasy | 5,657.7 ns | 40.22 ns | 37.62 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 226557
  bar [758.7, 514.2, 404.1, 188797.2, 4930, 5657.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 894.9 ns | 17.93 ns | 18.42 ns | 3.2 KB |
| Imposter | 589.0 ns | 11.79 ns | 26.38 ns | 2.82 KB |
| Mockolate | 448.0 ns | 9.04 ns | 9.68 ns | 1.84 KB |
| Moq | 200,078.8 ns | 851.57 ns | 796.55 ns | 13.73 KB |
| NSubstitute | 5,499.0 ns | 47.61 ns | 37.17 ns | 8.53 KB |
| FakeItEasy | 6,891.6 ns | 113.77 ns | 88.82 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 240095
  bar [894.9, 589, 448, 200078.8, 5499, 6891.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-04T03:22:20.303Z*
