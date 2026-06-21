---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 690.3 ns | 13.69 ns | 20.49 ns | 3.11 KB |
| Imposter | 483.2 ns | 9.55 ns | 14.00 ns | 2.66 KB |
| Mockolate | 365.8 ns | 7.26 ns | 6.79 ns | 1.91 KB |
| Moq | 136,792.8 ns | 1,372.48 ns | 1,283.82 ns | 13.14 KB |
| NSubstitute | 4,427.2 ns | 63.38 ns | 56.18 ns | 7.93 KB |
| FakeItEasy | 5,069.5 ns | 73.02 ns | 68.31 ns | 7.52 KB |

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
  y-axis "Time (ns)" 0 --> 164152
  bar [690.3, 483.2, 365.8, 136792.8, 4427.2, 5069.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 824.2 ns | 10.12 ns | 9.46 ns | 3.2 KB |
| Imposter | 541.3 ns | 7.12 ns | 5.56 ns | 2.82 KB |
| Mockolate | 424.0 ns | 8.24 ns | 8.81 ns | 1.95 KB |
| Moq | 145,543.3 ns | 2,103.87 ns | 1,756.82 ns | 13.73 KB |
| NSubstitute | 4,892.4 ns | 94.74 ns | 93.05 ns | 8.53 KB |
| FakeItEasy | 5,915.1 ns | 76.64 ns | 67.94 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 174652
  bar [824.2, 541.3, 424, 145543.3, 4892.4, 5915.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-21T03:36:43.702Z*
