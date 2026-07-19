---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 686.6 ns | 10.65 ns | 9.44 ns | 3.11 KB |
| Imposter | 489.2 ns | 3.69 ns | 3.27 ns | 2.66 KB |
| Mockolate | 345.9 ns | 2.65 ns | 2.35 ns | 1.8 KB |
| Moq | 136,612.6 ns | 1,261.12 ns | 1,179.65 ns | 13.24 KB |
| NSubstitute | 4,544.0 ns | 13.67 ns | 12.79 ns | 7.85 KB |
| FakeItEasy | 5,134.0 ns | 50.04 ns | 46.81 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163936
  bar [686.6, 489.2, 345.9, 136612.6, 4544, 5134]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 867.3 ns | 16.01 ns | 14.98 ns | 3.2 KB |
| Imposter | 555.4 ns | 5.51 ns | 4.88 ns | 2.82 KB |
| Mockolate | 393.7 ns | 3.22 ns | 3.01 ns | 1.84 KB |
| Moq | 143,254.1 ns | 915.86 ns | 764.78 ns | 13.73 KB |
| NSubstitute | 4,882.7 ns | 37.66 ns | 29.40 ns | 8.41 KB |
| FakeItEasy | 5,572.5 ns | 66.73 ns | 55.72 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 171905
  bar [867.3, 555.4, 393.7, 143254.1, 4882.7, 5572.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-19T03:27:20.624Z*
