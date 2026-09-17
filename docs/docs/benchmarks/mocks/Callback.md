---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 697.1 ns | 12.29 ns | 10.89 ns | 3.11 KB |
| Imposter | 511.6 ns | 8.11 ns | 7.59 ns | 2.66 KB |
| Mockolate | 374.1 ns | 7.41 ns | 7.93 ns | 1.8 KB |
| Moq | 188,094.5 ns | 2,017.88 ns | 1,887.52 ns | 13.14 KB |
| NSubstitute | 5,088.5 ns | 21.95 ns | 20.53 ns | 7.85 KB |
| FakeItEasy | 5,295.8 ns | 21.43 ns | 20.05 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 225714
  bar [697.1, 511.6, 374.1, 188094.5, 5088.5, 5295.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 901.1 ns | 8.13 ns | 6.79 ns | 3.2 KB |
| Imposter | 537.3 ns | 9.25 ns | 8.20 ns | 2.82 KB |
| Mockolate | 415.6 ns | 4.16 ns | 3.89 ns | 1.84 KB |
| Moq | 197,768.6 ns | 916.17 ns | 812.16 ns | 13.73 KB |
| NSubstitute | 5,592.1 ns | 37.17 ns | 34.77 ns | 8.41 KB |
| FakeItEasy | 6,426.4 ns | 84.80 ns | 79.32 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 237323
  bar [901.1, 537.3, 415.6, 197768.6, 5592.1, 6426.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-17T02:33:27.459Z*
