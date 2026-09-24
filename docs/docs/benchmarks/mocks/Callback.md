---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 713.3 ns | 13.94 ns | 14.32 ns | 3.11 KB |
| Imposter | 451.7 ns | 8.49 ns | 7.09 ns | 2.66 KB |
| Mockolate | 374.7 ns | 7.19 ns | 8.83 ns | 1.8 KB |
| Moq | 79,049.6 ns | 1,368.82 ns | 1,213.42 ns | 13.24 KB |
| NSubstitute | 4,301.2 ns | 74.72 ns | 69.89 ns | 7.85 KB |
| FakeItEasy | 3,776.8 ns | 64.82 ns | 60.63 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 94860
  bar [713.3, 451.7, 374.7, 79049.6, 4301.2, 3776.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 827.0 ns | 16.21 ns | 21.08 ns | 3.2 KB |
| Imposter | 523.2 ns | 9.06 ns | 8.03 ns | 2.82 KB |
| Mockolate | 423.8 ns | 8.39 ns | 9.32 ns | 1.84 KB |
| Moq | 82,506.9 ns | 562.36 ns | 439.06 ns | 13.71 KB |
| NSubstitute | 4,676.4 ns | 35.03 ns | 31.05 ns | 8.41 KB |
| FakeItEasy | 4,793.4 ns | 58.74 ns | 54.94 ns | 9.27 KB |

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
  y-axis "Time (ns)" 0 --> 99009
  bar [827, 523.2, 423.8, 82506.9, 4676.4, 4793.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-24T02:33:35.117Z*
