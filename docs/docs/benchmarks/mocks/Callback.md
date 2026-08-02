---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 775.5 ns | 6.23 ns | 5.82 ns | 3.11 KB |
| Imposter | 553.7 ns | 5.89 ns | 5.51 ns | 2.66 KB |
| Mockolate | 414.6 ns | 7.86 ns | 7.35 ns | 1.8 KB |
| Moq | 189,838.7 ns | 952.90 ns | 844.72 ns | 13.14 KB |
| NSubstitute | 4,972.2 ns | 15.72 ns | 14.71 ns | 7.85 KB |
| FakeItEasy | 5,700.4 ns | 30.00 ns | 28.06 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 227807
  bar [775.5, 553.7, 414.6, 189838.7, 4972.2, 5700.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 917.8 ns | 8.27 ns | 7.33 ns | 3.2 KB |
| Imposter | 641.2 ns | 9.48 ns | 8.87 ns | 2.82 KB |
| Mockolate | 470.1 ns | 4.57 ns | 4.05 ns | 1.84 KB |
| Moq | 197,112.8 ns | 845.25 ns | 790.64 ns | 13.73 KB |
| NSubstitute | 5,749.6 ns | 30.14 ns | 26.72 ns | 8.41 KB |
| FakeItEasy | 6,775.4 ns | 80.37 ns | 71.25 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 236536
  bar [917.8, 641.2, 470.1, 197112.8, 5749.6, 6775.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-02T03:23:38.806Z*
