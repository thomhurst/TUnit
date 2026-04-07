---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 746.7 ns | 14.03 ns | 14.40 ns | 3.06 KB |
| Imposter | 500.0 ns | 9.96 ns | 19.42 ns | 2.66 KB |
| Mockolate | 517.9 ns | 7.25 ns | 6.06 ns | 1.8 KB |
| Moq | 188,044.7 ns | 1,407.17 ns | 1,316.27 ns | 13.3 KB |
| NSubstitute | 4,661.9 ns | 52.37 ns | 48.99 ns | 7.93 KB |
| FakeItEasy | 5,463.0 ns | 75.66 ns | 70.77 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 225654
  bar [746.7, 500, 517.9, 188044.7, 4661.9, 5463]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 814.2 ns | 14.65 ns | 13.70 ns | 3.15 KB |
| Imposter | 598.2 ns | 11.57 ns | 11.36 ns | 2.82 KB |
| Mockolate | 676.7 ns | 13.37 ns | 19.59 ns | 2.13 KB |
| Moq | 193,932.5 ns | 2,062.51 ns | 1,929.28 ns | 13.73 KB |
| NSubstitute | 5,182.1 ns | 65.78 ns | 61.53 ns | 8.53 KB |
| FakeItEasy | 6,631.6 ns | 75.42 ns | 70.55 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 232719
  bar [814.2, 598.2, 676.7, 193932.5, 5182.1, 6631.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-07T03:21:31.527Z*
