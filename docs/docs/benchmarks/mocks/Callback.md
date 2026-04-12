---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 691.1 ns | 4.61 ns | 4.31 ns | 3.13 KB |
| Imposter | 451.0 ns | 2.05 ns | 1.71 ns | 2.66 KB |
| Mockolate | 501.2 ns | 2.49 ns | 2.08 ns | 1.8 KB |
| Moq | 184,914.1 ns | 874.79 ns | 818.28 ns | 13.14 KB |
| NSubstitute | 4,362.0 ns | 23.52 ns | 20.85 ns | 7.93 KB |
| FakeItEasy | 5,121.1 ns | 36.68 ns | 28.64 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221897
  bar [691.1, 451, 501.2, 184914.1, 4362, 5121.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 830.1 ns | 4.53 ns | 4.02 ns | 3.22 KB |
| Imposter | 536.3 ns | 5.00 ns | 4.68 ns | 2.82 KB |
| Mockolate | 646.7 ns | 5.90 ns | 4.93 ns | 2.13 KB |
| Moq | 190,990.8 ns | 700.04 ns | 620.57 ns | 13.73 KB |
| NSubstitute | 4,895.8 ns | 24.81 ns | 21.99 ns | 8.53 KB |
| FakeItEasy | 6,148.9 ns | 67.88 ns | 56.68 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 229189
  bar [830.1, 536.3, 646.7, 190990.8, 4895.8, 6148.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-12T03:28:39.462Z*
