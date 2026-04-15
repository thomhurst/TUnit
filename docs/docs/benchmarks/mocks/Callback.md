---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 710.1 ns | 8.63 ns | 8.08 ns | 3.13 KB |
| Imposter | 477.8 ns | 5.85 ns | 5.18 ns | 2.66 KB |
| Mockolate | 517.8 ns | 3.57 ns | 3.17 ns | 1.8 KB |
| Moq | 184,654.4 ns | 545.71 ns | 483.76 ns | 13.14 KB |
| NSubstitute | 4,571.4 ns | 21.65 ns | 19.19 ns | 7.93 KB |
| FakeItEasy | 5,192.1 ns | 38.29 ns | 35.81 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221586
  bar [710.1, 477.8, 517.8, 184654.4, 4571.4, 5192.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 828.0 ns | 11.95 ns | 11.18 ns | 3.22 KB |
| Imposter | 552.9 ns | 5.56 ns | 4.93 ns | 2.82 KB |
| Mockolate | 635.4 ns | 4.02 ns | 3.56 ns | 2.13 KB |
| Moq | 192,392.8 ns | 1,089.49 ns | 965.80 ns | 13.73 KB |
| NSubstitute | 5,056.6 ns | 37.15 ns | 32.93 ns | 8.53 KB |
| FakeItEasy | 6,393.1 ns | 67.25 ns | 52.50 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 230872
  bar [828, 552.9, 635.4, 192392.8, 5056.6, 6393.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-15T03:22:40.574Z*
