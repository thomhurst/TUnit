---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 680.2 ns | 8.94 ns | 7.93 ns | 3.13 KB |
| Imposter | 487.3 ns | 9.46 ns | 11.62 ns | 2.66 KB |
| Mockolate | 523.0 ns | 7.22 ns | 6.75 ns | 1.8 KB |
| Moq | 186,330.0 ns | 1,314.46 ns | 1,229.54 ns | 13.14 KB |
| NSubstitute | 4,551.5 ns | 52.21 ns | 48.84 ns | 7.93 KB |
| FakeItEasy | 5,411.1 ns | 51.76 ns | 45.89 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223596
  bar [680.2, 487.3, 523, 186330, 4551.5, 5411.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 905.4 ns | 17.62 ns | 16.49 ns | 3.22 KB |
| Imposter | 532.0 ns | 10.66 ns | 14.95 ns | 2.82 KB |
| Mockolate | 682.8 ns | 13.51 ns | 14.46 ns | 2.13 KB |
| Moq | 191,584.7 ns | 1,216.64 ns | 1,078.52 ns | 13.73 KB |
| NSubstitute | 5,140.2 ns | 90.33 ns | 80.07 ns | 8.53 KB |
| FakeItEasy | 6,140.7 ns | 100.33 ns | 78.33 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 229902
  bar [905.4, 532, 682.8, 191584.7, 5140.2, 6140.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-17T03:23:50.633Z*
