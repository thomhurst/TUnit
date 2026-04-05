---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 493.5 ns | 3.99 ns | 3.33 ns | 2.03 KB |
| Imposter | 769.1 ns | 14.98 ns | 15.38 ns | 6.12 KB |
| Mockolate | 433.3 ns | 2.76 ns | 2.58 ns | 2.03 KB |
| Moq | 411,028.5 ns | 2,394.81 ns | 2,122.94 ns | 28.66 KB |
| NSubstitute | 5,448.0 ns | 36.21 ns | 33.87 ns | 9.01 KB |
| FakeItEasy | 7,856.3 ns | 53.96 ns | 50.47 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 493235
  bar [493.5, 769.1, 433.3, 411028.5, 5448, 7856.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 696.3 ns | 3.26 ns | 3.05 ns | 2.7 KB |
| Imposter | 1,345.8 ns | 5.50 ns | 4.87 ns | 10.59 KB |
| Mockolate | 696.3 ns | 4.59 ns | 4.29 ns | 3.07 KB |
| Moq | 111,035.7 ns | 408.48 ns | 341.10 ns | 16.53 KB |
| NSubstitute | 11,663.6 ns | 30.37 ns | 26.92 ns | 20.5 KB |
| FakeItEasy | 7,863.1 ns | 39.96 ns | 33.37 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 133243
  bar [696.3, 1345.8, 696.3, 111035.7, 11663.6, 7863.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T11:44:06.333Z*
