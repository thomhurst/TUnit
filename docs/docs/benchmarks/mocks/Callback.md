---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 702.2 ns | 13.70 ns | 14.07 ns | 3.11 KB |
| Imposter | 500.9 ns | 9.64 ns | 13.20 ns | 2.66 KB |
| Mockolate | 380.9 ns | 3.95 ns | 3.69 ns | 1.8 KB |
| Moq | 188,892.5 ns | 1,709.03 ns | 1,598.63 ns | 13.25 KB |
| NSubstitute | 4,952.4 ns | 54.96 ns | 51.41 ns | 7.85 KB |
| FakeItEasy | 5,521.1 ns | 76.79 ns | 71.83 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 226671
  bar [702.2, 500.9, 380.9, 188892.5, 4952.4, 5521.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 860.0 ns | 5.15 ns | 4.57 ns | 3.2 KB |
| Imposter | 579.3 ns | 5.56 ns | 4.65 ns | 2.82 KB |
| Mockolate | 437.0 ns | 6.54 ns | 6.12 ns | 1.84 KB |
| Moq | 196,907.9 ns | 916.79 ns | 857.57 ns | 13.73 KB |
| NSubstitute | 5,613.4 ns | 61.09 ns | 57.14 ns | 8.41 KB |
| FakeItEasy | 6,458.5 ns | 127.89 ns | 152.24 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 236290
  bar [860, 579.3, 437, 196907.9, 5613.4, 6458.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-15T02:33:23.208Z*
