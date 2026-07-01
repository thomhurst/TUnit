---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 514.2 ns | 3.69 ns | 3.45 ns | 3.11 KB |
| Imposter | 374.6 ns | 2.41 ns | 2.14 ns | 2.66 KB |
| Mockolate | 264.9 ns | 1.11 ns | 1.04 ns | 1.8 KB |
| Moq | 107,668.3 ns | 796.43 ns | 706.02 ns | 13.29 KB |
| NSubstitute | 3,332.0 ns | 57.94 ns | 54.20 ns | 7.93 KB |
| FakeItEasy | 3,688.1 ns | 24.52 ns | 21.73 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 129202
  bar [514.2, 374.6, 264.9, 107668.3, 3332, 3688.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 617.3 ns | 5.42 ns | 4.81 ns | 3.2 KB |
| Imposter | 418.0 ns | 1.83 ns | 1.72 ns | 2.82 KB |
| Mockolate | 305.4 ns | 0.99 ns | 0.93 ns | 1.84 KB |
| Moq | 116,124.8 ns | 1,005.70 ns | 940.73 ns | 13.91 KB |
| NSubstitute | 3,657.8 ns | 25.85 ns | 24.18 ns | 8.53 KB |
| FakeItEasy | 4,452.1 ns | 38.01 ns | 35.56 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 139350
  bar [617.3, 418, 305.4, 116124.8, 3657.8, 4452.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-01T03:29:08.803Z*
