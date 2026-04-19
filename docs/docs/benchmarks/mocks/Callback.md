---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 782.3 ns | 12.02 ns | 11.24 ns | 3.13 KB |
| Imposter | 511.9 ns | 9.76 ns | 9.59 ns | 2.66 KB |
| Mockolate | 595.3 ns | 6.77 ns | 6.34 ns | 1.8 KB |
| Moq | 137,871.3 ns | 714.92 ns | 596.99 ns | 13.29 KB |
| NSubstitute | 4,400.5 ns | 24.06 ns | 21.33 ns | 7.93 KB |
| FakeItEasy | 5,242.5 ns | 50.94 ns | 47.65 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 165446
  bar [782.3, 511.9, 595.3, 137871.3, 4400.5, 5242.5]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 909.5 ns | 12.49 ns | 11.68 ns | 3.22 KB |
| Imposter | 569.6 ns | 9.86 ns | 9.23 ns | 2.82 KB |
| Mockolate | 729.3 ns | 10.94 ns | 10.23 ns | 2.13 KB |
| Moq | 144,852.4 ns | 1,480.50 ns | 1,236.29 ns | 13.75 KB |
| NSubstitute | 4,832.6 ns | 50.43 ns | 47.17 ns | 8.53 KB |
| FakeItEasy | 6,429.4 ns | 29.82 ns | 27.89 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173823
  bar [909.5, 569.6, 729.3, 144852.4, 4832.6, 6429.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-19T03:31:38.770Z*
