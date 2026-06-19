---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 642.5 ns | 1.54 ns | 1.37 ns | 3.11 KB |
| Imposter | 463.3 ns | 0.69 ns | 0.64 ns | 2.66 KB |
| Mockolate | 359.0 ns | 1.01 ns | 0.95 ns | 1.91 KB |
| Moq | 135,153.8 ns | 708.43 ns | 628.00 ns | 13.29 KB |
| NSubstitute | 4,216.8 ns | 21.53 ns | 16.81 ns | 7.93 KB |
| FakeItEasy | 4,614.5 ns | 15.36 ns | 13.61 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162185
  bar [642.5, 463.3, 359, 135153.8, 4216.8, 4614.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 809.7 ns | 4.42 ns | 3.92 ns | 3.2 KB |
| Imposter | 528.0 ns | 1.91 ns | 1.79 ns | 2.82 KB |
| Mockolate | 402.1 ns | 1.17 ns | 1.09 ns | 1.95 KB |
| Moq | 145,136.1 ns | 1,325.21 ns | 1,174.76 ns | 13.73 KB |
| NSubstitute | 4,561.8 ns | 17.19 ns | 15.24 ns | 8.53 KB |
| FakeItEasy | 5,495.7 ns | 29.35 ns | 27.45 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 174164
  bar [809.7, 528, 402.1, 145136.1, 4561.8, 5495.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-19T03:29:43.427Z*
