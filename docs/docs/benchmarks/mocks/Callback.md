---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 700.0 ns | 5.08 ns | 4.51 ns | 2.98 KB |
| Imposter | 538.8 ns | 9.29 ns | 8.23 ns | 2.66 KB |
| Mockolate | 397.0 ns | 2.71 ns | 2.40 ns | 1.89 KB |
| Moq | 185,777.2 ns | 1,201.65 ns | 1,124.03 ns | 13.14 KB |
| NSubstitute | 4,695.5 ns | 34.61 ns | 28.90 ns | 7.93 KB |
| FakeItEasy | 5,420.0 ns | 31.41 ns | 27.84 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 222933
  bar [700, 538.8, 397, 185777.2, 4695.5, 5420]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 872.7 ns | 12.54 ns | 11.73 ns | 3.06 KB |
| Imposter | 597.2 ns | 6.10 ns | 5.71 ns | 2.82 KB |
| Mockolate | 453.2 ns | 9.11 ns | 8.52 ns | 1.94 KB |
| Moq | 195,298.7 ns | 1,068.68 ns | 999.65 ns | 13.73 KB |
| NSubstitute | 5,159.7 ns | 28.24 ns | 25.04 ns | 8.53 KB |
| FakeItEasy | 6,626.5 ns | 55.71 ns | 52.11 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 234359
  bar [872.7, 597.2, 453.2, 195298.7, 5159.7, 6626.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-11T03:29:06.162Z*
