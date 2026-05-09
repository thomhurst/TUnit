---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 649.9 ns | 9.40 ns | 9.65 ns | 2.98 KB |
| Imposter | 508.6 ns | 9.98 ns | 12.26 ns | 2.66 KB |
| Mockolate | 385.2 ns | 6.27 ns | 5.86 ns | 1.89 KB |
| Moq | 187,775.6 ns | 640.95 ns | 535.22 ns | 13.14 KB |
| NSubstitute | 4,524.5 ns | 33.30 ns | 31.15 ns | 7.93 KB |
| FakeItEasy | 5,115.9 ns | 37.23 ns | 33.00 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 225331
  bar [649.9, 508.6, 385.2, 187775.6, 4524.5, 5115.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 743.5 ns | 9.48 ns | 8.86 ns | 3.06 KB |
| Imposter | 556.4 ns | 11.14 ns | 16.68 ns | 2.82 KB |
| Mockolate | 439.4 ns | 6.10 ns | 5.70 ns | 1.94 KB |
| Moq | 196,446.0 ns | 1,317.15 ns | 1,232.06 ns | 13.73 KB |
| NSubstitute | 5,126.1 ns | 65.61 ns | 61.37 ns | 8.53 KB |
| FakeItEasy | 6,338.6 ns | 82.81 ns | 73.41 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 235736
  bar [743.5, 556.4, 439.4, 196446, 5126.1, 6338.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-09T03:26:33.451Z*
