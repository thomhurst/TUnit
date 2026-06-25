---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 806.8 ns | 7.81 ns | 7.30 ns | 3.11 KB |
| Imposter | 571.4 ns | 10.59 ns | 9.91 ns | 2.66 KB |
| Mockolate | 463.4 ns | 3.69 ns | 3.08 ns | 1.8 KB |
| Moq | 140,883.7 ns | 1,376.31 ns | 1,287.40 ns | 13.29 KB |
| NSubstitute | 4,504.6 ns | 25.78 ns | 21.53 ns | 7.93 KB |
| FakeItEasy | 5,291.4 ns | 39.11 ns | 36.58 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 169061
  bar [806.8, 571.4, 463.4, 140883.7, 4504.6, 5291.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 973.6 ns | 6.68 ns | 6.25 ns | 3.2 KB |
| Imposter | 641.5 ns | 8.42 ns | 7.87 ns | 2.82 KB |
| Mockolate | 509.2 ns | 6.43 ns | 6.01 ns | 1.84 KB |
| Moq | 146,192.9 ns | 1,820.51 ns | 1,702.90 ns | 13.75 KB |
| NSubstitute | 5,050.6 ns | 20.82 ns | 17.39 ns | 8.53 KB |
| FakeItEasy | 6,536.2 ns | 21.92 ns | 20.50 ns | 9.41 KB |

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
  y-axis "Time (ns)" 0 --> 175432
  bar [973.6, 641.5, 509.2, 146192.9, 5050.6, 6536.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-25T03:27:42.911Z*
