---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 814.9 ns | 16.06 ns | 15.78 ns | 3.11 KB |
| Imposter | 528.9 ns | 9.35 ns | 8.75 ns | 2.66 KB |
| Mockolate | 391.6 ns | 6.43 ns | 7.40 ns | 1.8 KB |
| Moq | 186,858.7 ns | 1,044.84 ns | 977.34 ns | 13.14 KB |
| NSubstitute | 4,615.8 ns | 23.44 ns | 18.30 ns | 7.93 KB |
| FakeItEasy | 5,425.7 ns | 16.81 ns | 15.72 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 224231
  bar [814.9, 528.9, 391.6, 186858.7, 4615.8, 5425.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 854.5 ns | 9.52 ns | 8.44 ns | 3.2 KB |
| Imposter | 602.2 ns | 12.09 ns | 23.86 ns | 2.82 KB |
| Mockolate | 451.1 ns | 7.45 ns | 6.97 ns | 1.84 KB |
| Moq | 199,156.7 ns | 593.92 ns | 526.49 ns | 13.73 KB |
| NSubstitute | 5,418.7 ns | 28.35 ns | 26.51 ns | 8.53 KB |
| FakeItEasy | 7,060.1 ns | 37.74 ns | 35.30 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 238989
  bar [854.5, 602.2, 451.1, 199156.7, 5418.7, 7060.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-12T03:30:57.252Z*
