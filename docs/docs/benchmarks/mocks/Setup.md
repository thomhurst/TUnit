---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 547.9 ns | 10.35 ns | 10.63 ns | 2.34 KB |
| Imposter | 816.9 ns | 16.10 ns | 25.53 ns | 6.12 KB |
| Mockolate | 306.0 ns | 5.53 ns | 8.27 ns | 1.41 KB |
| Moq | 434,269.1 ns | 2,720.12 ns | 2,544.40 ns | 28.52 KB |
| NSubstitute | 5,655.1 ns | 98.18 ns | 120.57 ns | 9.06 KB |
| FakeItEasy | 8,761.6 ns | 157.78 ns | 147.58 ns | 10.56 KB |

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
  y-axis "Time (ns)" 0 --> 521123
  bar [547.9, 816.9, 306, 434269.1, 5655.1, 8761.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 789.6 ns | 15.76 ns | 25.90 ns | 3.15 KB |
| Imposter | 1,410.5 ns | 28.03 ns | 46.05 ns | 10.59 KB |
| Mockolate | 554.5 ns | 5.48 ns | 5.12 ns | 2.35 KB |
| Moq | 118,498.6 ns | 1,288.76 ns | 1,205.51 ns | 17.07 KB |
| NSubstitute | 12,198.8 ns | 139.78 ns | 123.91 ns | 20.34 KB |
| FakeItEasy | 7,943.8 ns | 153.50 ns | 136.08 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 142199
  bar [789.6, 1410.5, 554.5, 118498.6, 12198.8, 7943.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-24T03:21:14.704Z*
