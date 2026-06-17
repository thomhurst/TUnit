---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 643.2 ns | 3.73 ns | 3.12 ns | 3.11 KB |
| Imposter | 459.2 ns | 1.46 ns | 1.36 ns | 2.66 KB |
| Mockolate | 355.3 ns | 1.04 ns | 0.87 ns | 1.91 KB |
| Moq | 136,059.1 ns | 1,655.45 ns | 1,467.51 ns | 13.14 KB |
| NSubstitute | 4,093.4 ns | 18.71 ns | 16.59 ns | 7.93 KB |
| FakeItEasy | 4,571.5 ns | 34.49 ns | 32.26 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163271
  bar [643.2, 459.2, 355.3, 136059.1, 4093.4, 4571.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 790.1 ns | 4.79 ns | 4.24 ns | 3.2 KB |
| Imposter | 541.9 ns | 1.77 ns | 1.66 ns | 2.82 KB |
| Mockolate | 399.1 ns | 2.00 ns | 1.77 ns | 1.95 KB |
| Moq | 145,945.6 ns | 887.92 ns | 830.56 ns | 13.84 KB |
| NSubstitute | 4,590.9 ns | 22.92 ns | 20.32 ns | 8.53 KB |
| FakeItEasy | 5,495.5 ns | 28.70 ns | 23.97 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 175135
  bar [790.1, 541.9, 399.1, 145945.6, 4590.9, 5495.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-17T03:28:53.706Z*
