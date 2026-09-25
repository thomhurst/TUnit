---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 708.8 ns | 9.69 ns | 9.07 ns | 3.11 KB |
| Imposter | 489.7 ns | 9.73 ns | 15.15 ns | 2.66 KB |
| Mockolate | 388.1 ns | 7.75 ns | 8.61 ns | 1.8 KB |
| Moq | 185,881.1 ns | 758.35 ns | 672.26 ns | 13.14 KB |
| NSubstitute | 4,993.8 ns | 55.27 ns | 51.70 ns | 7.85 KB |
| FakeItEasy | 5,533.6 ns | 75.50 ns | 66.93 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223058
  bar [708.8, 489.7, 388.1, 185881.1, 4993.8, 5533.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 807.5 ns | 16.17 ns | 19.86 ns | 3.2 KB |
| Imposter | 547.1 ns | 10.95 ns | 12.61 ns | 2.82 KB |
| Mockolate | 444.7 ns | 8.88 ns | 10.22 ns | 1.84 KB |
| Moq | 197,985.6 ns | 946.48 ns | 839.03 ns | 13.7 KB |
| NSubstitute | 5,714.8 ns | 39.38 ns | 34.91 ns | 8.41 KB |
| FakeItEasy | 6,744.8 ns | 93.52 ns | 82.90 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 237583
  bar [807.5, 547.1, 444.7, 197985.6, 5714.8, 6744.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-25T02:32:28.119Z*
