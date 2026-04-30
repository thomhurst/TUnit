---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 419.8 ns | 1.56 ns | 1.30 ns | 2.01 KB |
| Imposter | 766.5 ns | 3.60 ns | 3.19 ns | 6.12 KB |
| Mockolate | 422.7 ns | 1.87 ns | 1.66 ns | 2.03 KB |
| Moq | 423,136.6 ns | 2,561.42 ns | 2,395.96 ns | 28.52 KB |
| NSubstitute | 5,337.3 ns | 20.96 ns | 19.61 ns | 9.06 KB |
| FakeItEasy | 8,136.8 ns | 61.87 ns | 57.87 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 507764
  bar [419.8, 766.5, 422.7, 423136.6, 5337.3, 8136.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 598.9 ns | 9.40 ns | 8.79 ns | 2.59 KB |
| Imposter | 1,419.0 ns | 5.72 ns | 5.07 ns | 10.59 KB |
| Mockolate | 688.8 ns | 13.75 ns | 13.50 ns | 3.07 KB |
| Moq | 113,185.4 ns | 980.63 ns | 917.28 ns | 16.64 KB |
| NSubstitute | 11,765.0 ns | 56.40 ns | 52.75 ns | 20.5 KB |
| FakeItEasy | 7,596.9 ns | 76.69 ns | 71.73 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 135823
  bar [598.9, 1419, 688.8, 113185.4, 11765, 7596.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-30T03:25:10.403Z*
