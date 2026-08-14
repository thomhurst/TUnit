---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 786.8 ns | 14.75 ns | 21.62 ns | 3.11 KB |
| Imposter | 578.3 ns | 10.90 ns | 16.65 ns | 2.66 KB |
| Mockolate | 430.4 ns | 6.55 ns | 6.12 ns | 1.8 KB |
| Moq | 137,034.5 ns | 2,241.41 ns | 1,986.95 ns | 13.26 KB |
| NSubstitute | 4,740.0 ns | 17.94 ns | 14.98 ns | 7.85 KB |
| FakeItEasy | 5,309.2 ns | 80.01 ns | 74.84 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 164442
  bar [786.8, 578.3, 430.4, 137034.5, 4740, 5309.2]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 951.1 ns | 10.33 ns | 9.66 ns | 3.2 KB |
| Imposter | 620.2 ns | 12.29 ns | 12.62 ns | 2.82 KB |
| Mockolate | 497.2 ns | 9.52 ns | 8.91 ns | 1.84 KB |
| Moq | 147,571.5 ns | 1,055.75 ns | 935.89 ns | 13.97 KB |
| NSubstitute | 5,532.4 ns | 14.80 ns | 13.12 ns | 8.41 KB |
| FakeItEasy | 6,488.5 ns | 48.43 ns | 45.30 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 177086
  bar [951.1, 620.2, 497.2, 147571.5, 5532.4, 6488.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-14T03:10:39.371Z*
