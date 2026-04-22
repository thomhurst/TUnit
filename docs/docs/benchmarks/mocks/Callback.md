---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 526.2 ns | 4.14 ns | 3.46 ns | 3.13 KB |
| Imposter | 360.0 ns | 0.80 ns | 0.71 ns | 2.66 KB |
| Mockolate | 403.4 ns | 1.23 ns | 1.15 ns | 1.8 KB |
| Moq | 104,098.9 ns | 823.09 ns | 687.32 ns | 13.29 KB |
| NSubstitute | 3,178.1 ns | 10.64 ns | 8.88 ns | 7.93 KB |
| FakeItEasy | 3,545.6 ns | 15.66 ns | 14.65 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 124919
  bar [526.2, 360, 403.4, 104098.9, 3178.1, 3545.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 595.7 ns | 1.09 ns | 0.97 ns | 3.22 KB |
| Imposter | 432.9 ns | 1.66 ns | 1.55 ns | 2.82 KB |
| Mockolate | 511.0 ns | 3.01 ns | 2.67 ns | 2.13 KB |
| Moq | 112,853.2 ns | 988.51 ns | 876.29 ns | 13.76 KB |
| NSubstitute | 3,532.8 ns | 14.78 ns | 13.82 ns | 8.53 KB |
| FakeItEasy | 4,244.4 ns | 23.22 ns | 21.72 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 135424
  bar [595.7, 432.9, 511, 112853.2, 3532.8, 4244.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-22T03:22:46.937Z*
