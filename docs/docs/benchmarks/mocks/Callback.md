---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 659.3 ns | 4.74 ns | 4.44 ns | 3.11 KB |
| Imposter | 450.1 ns | 0.96 ns | 0.89 ns | 2.66 KB |
| Mockolate | 354.5 ns | 1.59 ns | 1.48 ns | 1.8 KB |
| Moq | 185,813.2 ns | 1,411.52 ns | 1,251.28 ns | 13.14 KB |
| NSubstitute | 4,573.0 ns | 28.60 ns | 26.75 ns | 7.93 KB |
| FakeItEasy | 5,136.8 ns | 35.58 ns | 31.54 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 222976
  bar [659.3, 450.1, 354.5, 185813.2, 4573, 5136.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 811.2 ns | 16.07 ns | 32.47 ns | 3.2 KB |
| Imposter | 527.2 ns | 6.48 ns | 5.75 ns | 2.82 KB |
| Mockolate | 397.1 ns | 2.93 ns | 2.45 ns | 1.84 KB |
| Moq | 193,062.3 ns | 825.22 ns | 731.53 ns | 13.73 KB |
| NSubstitute | 5,103.9 ns | 41.58 ns | 34.72 ns | 8.53 KB |
| FakeItEasy | 6,507.6 ns | 56.35 ns | 49.96 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 231675
  bar [811.2, 527.2, 397.1, 193062.3, 5103.9, 6507.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-24T03:28:17.466Z*
