---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 640.6 ns | 2.64 ns | 2.34 ns | 3.13 KB |
| Imposter | 458.7 ns | 3.12 ns | 2.92 ns | 2.61 KB |
| Mockolate | 339.2 ns | 1.24 ns | 1.10 ns | 1.8 KB |
| Moq | 185,174.8 ns | 834.73 ns | 697.04 ns | 13.14 KB |
| NSubstitute | 4,869.3 ns | 22.45 ns | 18.75 ns | 7.85 KB |
| FakeItEasy | 5,214.4 ns | 45.43 ns | 42.50 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 222210
  bar [640.6, 458.7, 339.2, 185174.8, 4869.3, 5214.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 826.2 ns | 4.75 ns | 4.44 ns | 3.23 KB |
| Imposter | 538.6 ns | 5.05 ns | 4.73 ns | 2.77 KB |
| Mockolate | 393.7 ns | 1.91 ns | 1.79 ns | 1.84 KB |
| Moq | 195,580.9 ns | 1,191.87 ns | 1,056.56 ns | 13.7 KB |
| NSubstitute | 5,512.7 ns | 49.46 ns | 43.85 ns | 8.41 KB |
| FakeItEasy | 6,257.2 ns | 68.23 ns | 56.98 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 234698
  bar [826.2, 538.6, 393.7, 195580.9, 5512.7, 6257.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-16T03:11:30.127Z*
