---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 663.4 ns | 3.38 ns | 3.00 ns | 3.11 KB |
| Imposter | 477.5 ns | 2.71 ns | 2.40 ns | 2.66 KB |
| Mockolate | 348.5 ns | 3.69 ns | 3.45 ns | 1.8 KB |
| Moq | 136,794.3 ns | 1,187.94 ns | 1,111.20 ns | 13.24 KB |
| NSubstitute | 4,316.6 ns | 25.42 ns | 23.78 ns | 7.85 KB |
| FakeItEasy | 4,707.9 ns | 57.40 ns | 44.81 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 164154
  bar [663.4, 477.5, 348.5, 136794.3, 4316.6, 4707.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 807.5 ns | 3.01 ns | 2.51 ns | 3.2 KB |
| Imposter | 545.7 ns | 3.22 ns | 3.01 ns | 2.82 KB |
| Mockolate | 391.4 ns | 2.08 ns | 1.95 ns | 1.84 KB |
| Moq | 144,797.6 ns | 657.45 ns | 582.81 ns | 13.73 KB |
| NSubstitute | 4,971.5 ns | 27.01 ns | 23.95 ns | 8.41 KB |
| FakeItEasy | 5,644.7 ns | 43.08 ns | 38.19 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173758
  bar [807.5, 545.7, 391.4, 144797.6, 4971.5, 5644.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-29T03:20:13.661Z*
