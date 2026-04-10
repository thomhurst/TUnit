---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 804.2 ns | 16.09 ns | 28.18 ns | 3.13 KB |
| Imposter | 540.4 ns | 10.59 ns | 15.52 ns | 2.66 KB |
| Mockolate | 587.6 ns | 8.53 ns | 7.98 ns | 1.8 KB |
| Moq | 140,719.1 ns | 1,467.48 ns | 1,372.68 ns | 13.29 KB |
| NSubstitute | 4,414.2 ns | 25.90 ns | 24.22 ns | 7.93 KB |
| FakeItEasy | 4,868.8 ns | 33.38 ns | 29.59 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 168863
  bar [804.2, 540.4, 587.6, 140719.1, 4414.2, 4868.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 964.5 ns | 11.85 ns | 10.51 ns | 3.22 KB |
| Imposter | 617.4 ns | 12.37 ns | 21.98 ns | 2.82 KB |
| Mockolate | 759.8 ns | 13.89 ns | 13.00 ns | 2.13 KB |
| Moq | 145,952.9 ns | 988.36 ns | 924.51 ns | 13.75 KB |
| NSubstitute | 4,910.5 ns | 50.54 ns | 47.28 ns | 8.53 KB |
| FakeItEasy | 6,437.0 ns | 35.45 ns | 33.16 ns | 9.38 KB |

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
  y-axis "Time (ns)" 0 --> 175144
  bar [964.5, 617.4, 759.8, 145952.9, 4910.5, 6437]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-10T03:23:10.636Z*
