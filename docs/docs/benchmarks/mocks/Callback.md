---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 683.4 ns | 13.70 ns | 16.82 ns | 3.13 KB |
| Imposter | 464.4 ns | 9.11 ns | 9.75 ns | 2.66 KB |
| Mockolate | 505.2 ns | 5.77 ns | 5.40 ns | 1.8 KB |
| Moq | 182,018.6 ns | 913.00 ns | 809.35 ns | 13.14 KB |
| NSubstitute | 4,332.1 ns | 40.48 ns | 37.87 ns | 7.93 KB |
| FakeItEasy | 5,080.6 ns | 20.20 ns | 16.86 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218423
  bar [683.4, 464.4, 505.2, 182018.6, 4332.1, 5080.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 829.4 ns | 16.30 ns | 15.24 ns | 3.22 KB |
| Imposter | 548.6 ns | 10.80 ns | 10.10 ns | 2.82 KB |
| Mockolate | 665.6 ns | 10.40 ns | 9.73 ns | 2.13 KB |
| Moq | 192,517.3 ns | 1,279.34 ns | 1,196.70 ns | 13.73 KB |
| NSubstitute | 5,229.9 ns | 61.87 ns | 54.85 ns | 8.53 KB |
| FakeItEasy | 6,597.0 ns | 84.00 ns | 74.46 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 231021
  bar [829.4, 548.6, 665.6, 192517.3, 5229.9, 6597]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-09T03:21:47.332Z*
