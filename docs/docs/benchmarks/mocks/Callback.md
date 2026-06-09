---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 629.6 ns | 1.31 ns | 1.09 ns | 3.1 KB |
| Imposter | 444.4 ns | 0.96 ns | 0.85 ns | 2.66 KB |
| Mockolate | 341.6 ns | 1.17 ns | 1.04 ns | 1.91 KB |
| Moq | 181,560.5 ns | 944.70 ns | 837.45 ns | 13.14 KB |
| NSubstitute | 4,283.7 ns | 19.53 ns | 17.32 ns | 7.93 KB |
| FakeItEasy | 4,913.0 ns | 16.15 ns | 14.31 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 217873
  bar [629.6, 444.4, 341.6, 181560.5, 4283.7, 4913]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 784.9 ns | 2.35 ns | 2.20 ns | 3.2 KB |
| Imposter | 495.9 ns | 1.57 ns | 1.47 ns | 2.82 KB |
| Mockolate | 383.6 ns | 0.51 ns | 0.45 ns | 1.95 KB |
| Moq | 190,790.2 ns | 1,232.52 ns | 1,029.21 ns | 13.73 KB |
| NSubstitute | 5,022.2 ns | 23.33 ns | 20.68 ns | 8.53 KB |
| FakeItEasy | 6,115.9 ns | 58.00 ns | 51.41 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 228949
  bar [784.9, 495.9, 383.6, 190790.2, 5022.2, 6115.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-09T03:29:02.106Z*
