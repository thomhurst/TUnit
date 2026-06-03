---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 691.4 ns | 7.53 ns | 7.04 ns | 3.08 KB |
| Imposter | 480.1 ns | 6.02 ns | 5.63 ns | 2.66 KB |
| Mockolate | 370.9 ns | 7.18 ns | 7.98 ns | 1.91 KB |
| Moq | 187,417.4 ns | 1,817.18 ns | 1,699.79 ns | 13.14 KB |
| NSubstitute | 4,521.9 ns | 38.55 ns | 36.06 ns | 7.93 KB |
| FakeItEasy | 5,423.4 ns | 49.36 ns | 46.17 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 224901
  bar [691.4, 480.1, 370.9, 187417.4, 4521.9, 5423.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 859.9 ns | 11.91 ns | 11.14 ns | 3.16 KB |
| Imposter | 569.1 ns | 8.21 ns | 7.68 ns | 2.82 KB |
| Mockolate | 438.6 ns | 7.61 ns | 6.75 ns | 1.95 KB |
| Moq | 192,181.4 ns | 862.40 ns | 764.49 ns | 13.73 KB |
| NSubstitute | 5,170.2 ns | 56.63 ns | 52.98 ns | 8.53 KB |
| FakeItEasy | 6,293.7 ns | 86.09 ns | 76.32 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 230618
  bar [859.9, 569.1, 438.6, 192181.4, 5170.2, 6293.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-03T03:30:19.511Z*
