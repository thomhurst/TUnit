---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 702.4 ns | 3.76 ns | 3.14 ns | 3.08 KB |
| Imposter | 490.9 ns | 8.61 ns | 8.06 ns | 2.66 KB |
| Mockolate | 351.6 ns | 1.45 ns | 1.36 ns | 1.91 KB |
| Moq | 135,707.2 ns | 722.53 ns | 675.86 ns | 13.29 KB |
| NSubstitute | 4,436.6 ns | 52.43 ns | 49.05 ns | 7.93 KB |
| FakeItEasy | 4,958.7 ns | 20.11 ns | 16.79 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162849
  bar [702.4, 490.9, 351.6, 135707.2, 4436.6, 4958.7]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 818.5 ns | 6.69 ns | 5.93 ns | 3.16 KB |
| Imposter | 571.3 ns | 11.05 ns | 13.57 ns | 2.82 KB |
| Mockolate | 417.4 ns | 4.44 ns | 3.94 ns | 1.95 KB |
| Moq | 144,831.0 ns | 1,104.83 ns | 979.41 ns | 13.73 KB |
| NSubstitute | 4,707.6 ns | 66.28 ns | 58.76 ns | 8.53 KB |
| FakeItEasy | 6,124.1 ns | 84.31 ns | 74.74 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 173798
  bar [818.5, 571.3, 417.4, 144831, 4707.6, 6124.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-31T03:32:45.264Z*
