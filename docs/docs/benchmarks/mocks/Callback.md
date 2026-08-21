---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 687.7 ns | 5.27 ns | 4.93 ns | 3.11 KB |
| Imposter | 478.4 ns | 4.26 ns | 3.78 ns | 2.66 KB |
| Mockolate | 353.3 ns | 1.58 ns | 1.48 ns | 1.8 KB |
| Moq | 136,486.8 ns | 1,369.80 ns | 1,214.29 ns | 13.14 KB |
| NSubstitute | 4,610.7 ns | 90.61 ns | 70.75 ns | 7.85 KB |
| FakeItEasy | 4,958.1 ns | 75.45 ns | 70.57 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163785
  bar [687.7, 478.4, 353.3, 136486.8, 4610.7, 4958.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 807.2 ns | 6.44 ns | 6.03 ns | 3.2 KB |
| Imposter | 549.0 ns | 3.54 ns | 3.31 ns | 2.82 KB |
| Mockolate | 415.7 ns | 1.64 ns | 1.37 ns | 1.84 KB |
| Moq | 146,038.6 ns | 982.21 ns | 918.76 ns | 13.73 KB |
| NSubstitute | 5,202.5 ns | 68.75 ns | 64.31 ns | 8.41 KB |
| FakeItEasy | 5,758.8 ns | 67.56 ns | 63.20 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 175247
  bar [807.2, 549, 415.7, 146038.6, 5202.5, 5758.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-21T02:46:27.792Z*
