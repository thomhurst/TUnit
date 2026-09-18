---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 683.7 ns | 3.19 ns | 2.83 ns | 3.11 KB |
| Imposter | 458.5 ns | 1.86 ns | 1.74 ns | 2.66 KB |
| Mockolate | 339.7 ns | 1.49 ns | 1.39 ns | 1.8 KB |
| Moq | 132,929.9 ns | 638.36 ns | 597.12 ns | 13.29 KB |
| NSubstitute | 4,507.2 ns | 41.64 ns | 36.92 ns | 7.85 KB |
| FakeItEasy | 4,596.7 ns | 34.94 ns | 30.98 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 159516
  bar [683.7, 458.5, 339.7, 132929.9, 4507.2, 4596.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 819.8 ns | 2.66 ns | 2.36 ns | 3.2 KB |
| Imposter | 544.7 ns | 1.56 ns | 1.46 ns | 2.82 KB |
| Mockolate | 380.5 ns | 0.82 ns | 0.72 ns | 1.84 KB |
| Moq | 141,911.6 ns | 469.36 ns | 391.93 ns | 13.75 KB |
| NSubstitute | 5,017.9 ns | 48.40 ns | 40.42 ns | 8.41 KB |
| FakeItEasy | 5,614.5 ns | 94.87 ns | 88.74 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 170294
  bar [819.8, 544.7, 380.5, 141911.6, 5017.9, 5614.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-18T02:32:22.133Z*
