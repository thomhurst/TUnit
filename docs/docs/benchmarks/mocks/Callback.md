---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 739.2 ns | 3.20 ns | 3.00 ns | 3.1 KB |
| Imposter | 513.3 ns | 6.41 ns | 6.00 ns | 2.66 KB |
| Mockolate | 401.2 ns | 5.92 ns | 5.24 ns | 1.91 KB |
| Moq | 184,851.4 ns | 1,626.92 ns | 1,521.82 ns | 13.14 KB |
| NSubstitute | 4,496.4 ns | 32.61 ns | 27.23 ns | 7.93 KB |
| FakeItEasy | 5,641.7 ns | 33.21 ns | 31.06 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221822
  bar [739.2, 513.3, 401.2, 184851.4, 4496.4, 5641.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 853.4 ns | 11.91 ns | 10.56 ns | 3.2 KB |
| Imposter | 574.4 ns | 11.39 ns | 17.74 ns | 2.82 KB |
| Mockolate | 438.9 ns | 8.60 ns | 14.61 ns | 1.95 KB |
| Moq | 195,736.3 ns | 1,942.26 ns | 1,816.80 ns | 13.85 KB |
| NSubstitute | 5,515.9 ns | 34.35 ns | 30.45 ns | 8.53 KB |
| FakeItEasy | 6,791.3 ns | 112.21 ns | 99.47 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 234884
  bar [853.4, 574.4, 438.9, 195736.3, 5515.9, 6791.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-10T03:28:13.506Z*
