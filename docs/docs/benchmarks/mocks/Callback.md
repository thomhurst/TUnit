---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 676.6 ns | 4.45 ns | 3.95 ns | 3.11 KB |
| Imposter | 461.0 ns | 0.92 ns | 0.77 ns | 2.66 KB |
| Mockolate | 341.8 ns | 3.14 ns | 2.94 ns | 1.8 KB |
| Moq | 136,313.7 ns | 660.22 ns | 551.31 ns | 13.29 KB |
| NSubstitute | 4,274.1 ns | 54.17 ns | 50.67 ns | 7.85 KB |
| FakeItEasy | 4,523.9 ns | 34.10 ns | 26.62 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163577
  bar [676.6, 461, 341.8, 136313.7, 4274.1, 4523.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 767.9 ns | 3.07 ns | 2.56 ns | 3.2 KB |
| Imposter | 536.4 ns | 2.39 ns | 2.00 ns | 2.82 KB |
| Mockolate | 396.3 ns | 3.89 ns | 3.45 ns | 1.84 KB |
| Moq | 143,569.2 ns | 1,488.84 ns | 1,319.82 ns | 13.73 KB |
| NSubstitute | 4,988.9 ns | 91.18 ns | 85.29 ns | 8.41 KB |
| FakeItEasy | 5,841.9 ns | 100.62 ns | 94.12 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 172284
  bar [767.9, 536.4, 396.3, 143569.2, 4988.9, 5841.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-25T03:20:44.831Z*
