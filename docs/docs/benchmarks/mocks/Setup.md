---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 592.7 ns | 2.97 ns | 2.48 ns | 2.15 KB |
| Imposter | 841.0 ns | 6.31 ns | 5.60 ns | 6.12 KB |
| Mockolate | 432.4 ns | 1.52 ns | 1.42 ns | 2.04 KB |
| Moq | 301,051.1 ns | 2,548.10 ns | 2,383.50 ns | 28.52 KB |
| NSubstitute | 5,174.9 ns | 36.20 ns | 32.09 ns | 9.06 KB |
| FakeItEasy | 6,911.1 ns | 44.70 ns | 37.32 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 361262
  bar [592.7, 841, 432.4, 301051.1, 5174.9, 6911.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 897.1 ns | 5.23 ns | 4.89 ns | 2.98 KB |
| Imposter | 1,327.8 ns | 8.54 ns | 7.57 ns | 10.59 KB |
| Mockolate | 645.2 ns | 2.54 ns | 2.37 ns | 3.05 KB |
| Moq | 87,210.5 ns | 452.71 ns | 401.32 ns | 16.53 KB |
| NSubstitute | 10,642.0 ns | 91.11 ns | 85.22 ns | 20.31 KB |
| FakeItEasy | 6,644.8 ns | 61.97 ns | 54.94 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 104653
  bar [897.1, 1327.8, 645.2, 87210.5, 10642, 6644.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T01:06:26.815Z*
