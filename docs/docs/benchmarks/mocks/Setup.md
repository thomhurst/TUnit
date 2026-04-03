---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 519.0 ns | 2.05 ns | 2.74 ns | 1.99 KB |
| Imposter | 757.9 ns | 4.50 ns | 3.76 ns | 6.12 KB |
| Mockolate | 422.9 ns | 7.31 ns | 6.48 ns | 2.01 KB |
| Moq | 420,967.6 ns | 3,803.31 ns | 3,557.62 ns | 28.52 KB |
| NSubstitute | 5,532.5 ns | 28.37 ns | 25.15 ns | 9.01 KB |
| FakeItEasy | 8,149.2 ns | 162.69 ns | 205.75 ns | 10.57 KB |

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
  y-axis "Time (ns)" 0 --> 505162
  bar [519, 757.9, 422.9, 420967.6, 5532.5, 8149.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 845.1 ns | 16.56 ns | 17.72 ns | 2.75 KB |
| Imposter | 1,352.8 ns | 27.01 ns | 36.97 ns | 10.59 KB |
| Mockolate | 663.3 ns | 7.55 ns | 6.31 ns | 3.05 KB |
| Moq | 113,979.4 ns | 398.79 ns | 353.51 ns | 16.53 KB |
| NSubstitute | 11,534.6 ns | 229.62 ns | 290.40 ns | 20.31 KB |
| FakeItEasy | 7,531.7 ns | 62.04 ns | 55.00 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136776
  bar [845.1, 1352.8, 663.3, 113979.4, 11534.6, 7531.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-03T03:23:45.860Z*
