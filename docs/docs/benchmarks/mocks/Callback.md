---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 494.3 ns | 9.28 ns | 8.68 ns | 2.98 KB |
| Imposter | 379.1 ns | 4.77 ns | 4.46 ns | 2.66 KB |
| Mockolate | 446.5 ns | 7.45 ns | 6.97 ns | 2.53 KB |
| Moq | 106,647.5 ns | 815.78 ns | 723.17 ns | 13.29 KB |
| NSubstitute | 3,273.6 ns | 64.55 ns | 66.29 ns | 7.93 KB |
| FakeItEasy | 3,897.4 ns | 73.56 ns | 68.81 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 127977
  bar [494.3, 379.1, 446.5, 106647.5, 3273.6, 3897.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 540.3 ns | 5.05 ns | 4.48 ns | 3.06 KB |
| Imposter | 427.9 ns | 2.04 ns | 1.70 ns | 2.82 KB |
| Mockolate | 437.3 ns | 2.80 ns | 2.62 ns | 2.58 KB |
| Moq | 112,792.1 ns | 819.08 ns | 726.10 ns | 13.76 KB |
| NSubstitute | 3,696.3 ns | 57.73 ns | 51.18 ns | 8.53 KB |
| FakeItEasy | 4,393.3 ns | 56.70 ns | 53.04 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 135351
  bar [540.3, 427.9, 437.3, 112792.1, 3696.3, 4393.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-02T03:24:38.193Z*
