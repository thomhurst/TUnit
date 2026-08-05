---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 695.4 ns | 13.06 ns | 12.22 ns | 3.11 KB |
| Imposter | 488.0 ns | 9.43 ns | 9.69 ns | 2.66 KB |
| Mockolate | 372.8 ns | 6.68 ns | 6.25 ns | 1.8 KB |
| Moq | 186,598.9 ns | 1,202.36 ns | 1,124.69 ns | 13.14 KB |
| NSubstitute | 4,850.6 ns | 46.72 ns | 43.71 ns | 7.85 KB |
| FakeItEasy | 5,339.9 ns | 42.97 ns | 38.09 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223919
  bar [695.4, 488, 372.8, 186598.9, 4850.6, 5339.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 837.5 ns | 9.01 ns | 8.42 ns | 3.2 KB |
| Imposter | 575.9 ns | 11.05 ns | 12.72 ns | 2.82 KB |
| Mockolate | 444.9 ns | 7.55 ns | 6.30 ns | 1.84 KB |
| Moq | 198,512.1 ns | 2,021.06 ns | 1,890.50 ns | 13.73 KB |
| NSubstitute | 5,975.1 ns | 44.19 ns | 41.33 ns | 8.41 KB |
| FakeItEasy | 6,523.8 ns | 88.78 ns | 78.70 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 238215
  bar [837.5, 575.9, 444.9, 198512.1, 5975.1, 6523.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-05T03:21:19.181Z*
