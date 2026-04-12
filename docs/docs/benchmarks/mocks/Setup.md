---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 562.6 ns | 10.73 ns | 10.03 ns | 2.34 KB |
| Imposter | 794.3 ns | 13.02 ns | 12.79 ns | 6.12 KB |
| Mockolate | 450.0 ns | 3.78 ns | 3.54 ns | 2.03 KB |
| Moq | 320,429.8 ns | 2,392.51 ns | 2,120.90 ns | 28.56 KB |
| NSubstitute | 5,362.3 ns | 33.09 ns | 30.96 ns | 9.01 KB |
| FakeItEasy | 7,563.8 ns | 56.66 ns | 53.00 ns | 10.46 KB |

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
  y-axis "Time (ns)" 0 --> 384516
  bar [562.6, 794.3, 450, 320429.8, 5362.3, 7563.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 764.7 ns | 5.88 ns | 5.50 ns | 2.93 KB |
| Imposter | 1,402.5 ns | 27.38 ns | 34.62 ns | 10.59 KB |
| Mockolate | 727.2 ns | 8.41 ns | 7.87 ns | 3.07 KB |
| Moq | 84,423.3 ns | 262.22 ns | 218.96 ns | 16.53 KB |
| NSubstitute | 11,405.8 ns | 41.21 ns | 36.54 ns | 20.31 KB |
| FakeItEasy | 7,430.5 ns | 33.62 ns | 29.80 ns | 11.72 KB |

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
  y-axis "Time (ns)" 0 --> 101308
  bar [764.7, 1402.5, 727.2, 84423.3, 11405.8, 7430.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-12T03:28:39.462Z*
