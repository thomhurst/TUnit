---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 608.5 ns | 8.76 ns | 8.19 ns | 2.34 KB |
| Imposter | 867.2 ns | 16.69 ns | 21.70 ns | 6.12 KB |
| Mockolate | 355.7 ns | 7.18 ns | 10.52 ns | 1.65 KB |
| Moq | 333,046.2 ns | 3,900.93 ns | 3,648.93 ns | 28.67 KB |
| NSubstitute | 5,307.8 ns | 53.55 ns | 50.09 ns | 9.01 KB |
| FakeItEasy | 7,719.0 ns | 65.41 ns | 61.19 ns | 10.46 KB |

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
  y-axis "Time (ns)" 0 --> 399656
  bar [608.5, 867.2, 355.7, 333046.2, 5307.8, 7719]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 948.2 ns | 18.31 ns | 17.13 ns | 3.15 KB |
| Imposter | 1,432.8 ns | 23.89 ns | 19.95 ns | 10.59 KB |
| Mockolate | 618.0 ns | 11.94 ns | 11.73 ns | 2.6 KB |
| Moq | 86,199.4 ns | 420.95 ns | 393.76 ns | 16.53 KB |
| NSubstitute | 11,248.5 ns | 72.02 ns | 63.84 ns | 20.49 KB |
| FakeItEasy | 7,494.7 ns | 135.78 ns | 127.01 ns | 11.72 KB |

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
  y-axis "Time (ns)" 0 --> 103440
  bar [948.2, 1432.8, 618, 86199.4, 11248.5, 7494.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-21T03:36:43.702Z*
