---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 779.8 ns | 15.23 ns | 21.85 ns | 3.11 KB |
| Imposter | 555.6 ns | 11.05 ns | 18.77 ns | 2.66 KB |
| Mockolate | 415.8 ns | 8.30 ns | 10.50 ns | 1.8 KB |
| Moq | 196,039.9 ns | 3,920.40 ns | 6,866.26 ns | 13.27 KB |
| NSubstitute | 4,652.9 ns | 86.28 ns | 84.74 ns | 7.93 KB |
| FakeItEasy | 5,550.9 ns | 36.23 ns | 32.12 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 235248
  bar [779.8, 555.6, 415.8, 196039.9, 4652.9, 5550.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 901.4 ns | 17.20 ns | 17.67 ns | 3.2 KB |
| Imposter | 782.8 ns | 15.48 ns | 24.99 ns | 2.82 KB |
| Mockolate | 462.7 ns | 9.31 ns | 12.43 ns | 1.84 KB |
| Moq | 200,198.9 ns | 1,131.90 ns | 1,058.78 ns | 13.73 KB |
| NSubstitute | 5,486.9 ns | 108.36 ns | 96.06 ns | 8.53 KB |
| FakeItEasy | 7,071.5 ns | 44.30 ns | 41.44 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 240239
  bar [901.4, 782.8, 462.7, 200198.9, 5486.9, 7071.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-08T03:21:22.090Z*
