---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 579.7 ns | 5.64 ns | 4.71 ns | 3.11 KB |
| Imposter | 387.4 ns | 1.15 ns | 1.07 ns | 2.66 KB |
| Mockolate | 312.9 ns | 0.88 ns | 0.82 ns | 1.8 KB |
| Moq | 77,420.8 ns | 524.34 ns | 490.47 ns | 13.28 KB |
| NSubstitute | 3,670.7 ns | 58.44 ns | 54.66 ns | 7.85 KB |
| FakeItEasy | 3,257.2 ns | 32.51 ns | 30.41 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 92905
  bar [579.7, 387.4, 312.9, 77420.8, 3670.7, 3257.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 701.4 ns | 7.01 ns | 5.85 ns | 3.2 KB |
| Imposter | 443.2 ns | 3.21 ns | 2.68 ns | 2.82 KB |
| Mockolate | 357.0 ns | 4.52 ns | 4.01 ns | 1.84 KB |
| Moq | 78,726.4 ns | 1,017.77 ns | 902.23 ns | 13.71 KB |
| NSubstitute | 4,056.1 ns | 55.33 ns | 51.76 ns | 8.41 KB |
| FakeItEasy | 4,049.5 ns | 30.04 ns | 25.08 ns | 9.27 KB |

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
  y-axis "Time (ns)" 0 --> 94472
  bar [701.4, 443.2, 357, 78726.4, 4056.1, 4049.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-03T03:22:34.236Z*
