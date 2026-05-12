---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 605.6 ns | 3.09 ns | 2.74 ns | 2.98 KB |
| Imposter | 484.0 ns | 6.84 ns | 6.06 ns | 2.66 KB |
| Mockolate | 364.9 ns | 5.97 ns | 5.58 ns | 1.91 KB |
| Moq | 182,961.2 ns | 905.32 ns | 846.84 ns | 13.14 KB |
| NSubstitute | 4,297.8 ns | 24.79 ns | 23.19 ns | 7.93 KB |
| FakeItEasy | 5,108.9 ns | 46.71 ns | 41.40 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 219554
  bar [605.6, 484, 364.9, 182961.2, 4297.8, 5108.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 719.1 ns | 8.89 ns | 8.32 ns | 3.06 KB |
| Imposter | 527.7 ns | 10.21 ns | 11.35 ns | 2.82 KB |
| Mockolate | 410.7 ns | 3.76 ns | 3.34 ns | 1.95 KB |
| Moq | 190,822.6 ns | 644.62 ns | 538.29 ns | 13.73 KB |
| NSubstitute | 4,999.6 ns | 65.05 ns | 60.85 ns | 8.53 KB |
| FakeItEasy | 6,298.8 ns | 70.38 ns | 58.77 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 228988
  bar [719.1, 527.7, 410.7, 190822.6, 4999.6, 6298.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-12T03:27:02.666Z*
