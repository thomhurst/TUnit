---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 542.2 ns | 4.01 ns | 3.55 ns | 2.34 KB |
| Imposter | 777.7 ns | 4.92 ns | 4.36 ns | 6.12 KB |
| Mockolate | 328.5 ns | 3.07 ns | 2.72 ns | 1.41 KB |
| Moq | 301,488.3 ns | 2,223.31 ns | 1,970.91 ns | 28.52 KB |
| NSubstitute | 5,315.1 ns | 63.31 ns | 59.22 ns | 9.01 KB |
| FakeItEasy | 7,089.2 ns | 47.34 ns | 41.97 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 361786
  bar [542.2, 777.7, 328.5, 301488.3, 5315.1, 7089.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 807.8 ns | 5.72 ns | 5.35 ns | 3.15 KB |
| Imposter | 1,391.3 ns | 24.21 ns | 22.65 ns | 10.59 KB |
| Mockolate | 565.4 ns | 4.81 ns | 4.50 ns | 2.35 KB |
| Moq | 87,912.4 ns | 728.63 ns | 645.91 ns | 16.53 KB |
| NSubstitute | 10,799.3 ns | 188.72 ns | 176.53 ns | 20.31 KB |
| FakeItEasy | 6,970.8 ns | 97.95 ns | 86.83 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 105495
  bar [807.8, 1391.3, 565.4, 87912.4, 10799.3, 6970.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-09T03:02:07.270Z*
