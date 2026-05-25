---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 663.5 ns | 5.22 ns | 4.36 ns | 3.08 KB |
| Imposter | 458.6 ns | 1.69 ns | 1.49 ns | 2.66 KB |
| Mockolate | 360.1 ns | 4.22 ns | 3.52 ns | 1.91 KB |
| Moq | 186,068.5 ns | 950.98 ns | 843.02 ns | 13.14 KB |
| NSubstitute | 4,532.7 ns | 42.41 ns | 39.67 ns | 7.93 KB |
| FakeItEasy | 5,121.4 ns | 42.55 ns | 39.80 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223283
  bar [663.5, 458.6, 360.1, 186068.5, 4532.7, 5121.4]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 771.8 ns | 3.49 ns | 3.27 ns | 3.16 KB |
| Imposter | 519.9 ns | 2.13 ns | 1.78 ns | 2.82 KB |
| Mockolate | 399.6 ns | 4.33 ns | 4.05 ns | 1.95 KB |
| Moq | 195,338.2 ns | 462.94 ns | 386.58 ns | 13.73 KB |
| NSubstitute | 4,996.4 ns | 56.33 ns | 52.70 ns | 8.53 KB |
| FakeItEasy | 6,133.9 ns | 83.26 ns | 73.81 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 234406
  bar [771.8, 519.9, 399.6, 195338.2, 4996.4, 6133.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-25T03:29:24.567Z*
