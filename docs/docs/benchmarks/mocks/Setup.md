---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 565.9 ns | 7.34 ns | 6.87 ns | 2.34 KB |
| Imposter | 762.3 ns | 3.81 ns | 3.38 ns | 6.12 KB |
| Mockolate | 459.5 ns | 4.26 ns | 3.98 ns | 2.03 KB |
| Moq | 300,222.8 ns | 1,498.93 ns | 1,170.27 ns | 28.52 KB |
| NSubstitute | 5,070.1 ns | 15.40 ns | 12.86 ns | 9.01 KB |
| FakeItEasy | 6,881.7 ns | 28.24 ns | 23.58 ns | 10.45 KB |

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
  title "Setup Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 360268
  bar [565.9, 762.3, 459.5, 300222.8, 5070.1, 6881.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 737.1 ns | 2.89 ns | 2.70 ns | 2.93 KB |
| Imposter | 1,349.9 ns | 3.86 ns | 3.01 ns | 10.59 KB |
| Mockolate | 728.1 ns | 2.23 ns | 1.97 ns | 3.07 KB |
| Moq | 88,121.1 ns | 1,125.07 ns | 1,052.39 ns | 16.53 KB |
| NSubstitute | 10,766.7 ns | 36.78 ns | 34.40 ns | 20.5 KB |
| FakeItEasy | 6,719.9 ns | 38.64 ns | 32.27 ns | 11.71 KB |

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
  title "Setup (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 105746
  bar [737.1, 1349.9, 728.1, 88121.1, 10766.7, 6719.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-13T03:23:34.678Z*
