---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 656.8 ns | 13.05 ns | 12.82 ns | 3.11 KB |
| Imposter | 472.3 ns | 9.38 ns | 8.78 ns | 2.66 KB |
| Mockolate | 351.2 ns | 4.45 ns | 3.95 ns | 1.8 KB |
| Moq | 183,245.3 ns | 1,620.89 ns | 1,516.18 ns | 13.15 KB |
| NSubstitute | 4,738.9 ns | 21.96 ns | 19.47 ns | 7.85 KB |
| FakeItEasy | 5,388.2 ns | 51.83 ns | 48.48 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 219895
  bar [656.8, 472.3, 351.2, 183245.3, 4738.9, 5388.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 798.3 ns | 13.49 ns | 11.96 ns | 3.2 KB |
| Imposter | 535.3 ns | 5.98 ns | 5.59 ns | 2.82 KB |
| Mockolate | 432.7 ns | 8.71 ns | 12.21 ns | 1.84 KB |
| Moq | 194,926.0 ns | 1,005.98 ns | 891.78 ns | 13.73 KB |
| NSubstitute | 5,577.4 ns | 28.47 ns | 25.24 ns | 8.41 KB |
| FakeItEasy | 6,622.6 ns | 57.63 ns | 53.91 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 233912
  bar [798.3, 535.3, 432.7, 194926, 5577.4, 6622.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-21T03:22:31.280Z*
