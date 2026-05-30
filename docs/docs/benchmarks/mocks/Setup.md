---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 538.8 ns | 10.51 ns | 10.80 ns | 2.31 KB |
| Imposter | 822.0 ns | 15.73 ns | 14.71 ns | 6.12 KB |
| Mockolate | 335.0 ns | 6.50 ns | 6.67 ns | 1.65 KB |
| Moq | 426,343.4 ns | 1,968.97 ns | 1,644.18 ns | 28.7 KB |
| NSubstitute | 5,797.3 ns | 61.55 ns | 57.58 ns | 9.01 KB |
| FakeItEasy | 9,151.1 ns | 166.04 ns | 147.19 ns | 10.64 KB |

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
  y-axis "Time (ns)" 0 --> 511613
  bar [538.8, 822, 335, 426343.4, 5797.3, 9151.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 767.5 ns | 15.26 ns | 19.30 ns | 3.09 KB |
| Imposter | 1,451.7 ns | 29.06 ns | 40.73 ns | 10.59 KB |
| Mockolate | 590.5 ns | 11.67 ns | 17.11 ns | 2.6 KB |
| Moq | 118,294.1 ns | 830.14 ns | 735.90 ns | 16.53 KB |
| NSubstitute | 12,047.1 ns | 90.94 ns | 80.62 ns | 20.31 KB |
| FakeItEasy | 8,051.4 ns | 78.96 ns | 70.00 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 141953
  bar [767.5, 1451.7, 590.5, 118294.1, 12047.1, 8051.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-30T03:25:40.021Z*
