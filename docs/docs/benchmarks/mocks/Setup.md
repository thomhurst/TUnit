---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 570.6 ns | 3.99 ns | 3.54 ns | 1.99 KB |
| Imposter | 869.7 ns | 13.09 ns | 12.24 ns | 6.12 KB |
| Mockolate | 483.5 ns | 3.67 ns | 3.25 ns | 2.01 KB |
| Moq | 430,129.0 ns | 741.21 ns | 618.95 ns | 28.87 KB |
| NSubstitute | 5,671.9 ns | 49.19 ns | 43.61 ns | 9.06 KB |
| FakeItEasy | 8,448.2 ns | 67.32 ns | 62.97 ns | 10.6 KB |

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
  y-axis "Time (ns)" 0 --> 516155
  bar [570.6, 869.7, 483.5, 430129, 5671.9, 8448.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 827.4 ns | 6.43 ns | 6.02 ns | 2.75 KB |
| Imposter | 1,400.4 ns | 12.10 ns | 11.32 ns | 10.59 KB |
| Mockolate | 727.7 ns | 8.53 ns | 7.98 ns | 3.05 KB |
| Moq | 114,345.2 ns | 510.06 ns | 477.11 ns | 16.53 KB |
| NSubstitute | 12,662.0 ns | 122.79 ns | 102.53 ns | 20.31 KB |
| FakeItEasy | 8,066.6 ns | 125.46 ns | 111.21 ns | 11.85 KB |

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
  y-axis "Time (ns)" 0 --> 137215
  bar [827.4, 1400.4, 727.7, 114345.2, 12662, 8066.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-01T03:22:34.139Z*
