---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 532.4 ns | 10.07 ns | 13.09 ns | 2.34 KB |
| Imposter | 683.9 ns | 13.69 ns | 17.80 ns | 6.12 KB |
| Mockolate | 301.6 ns | 6.11 ns | 7.03 ns | 1.41 KB |
| Moq | 188,613.6 ns | 2,188.59 ns | 1,940.13 ns | 28.46 KB |
| NSubstitute | 5,683.3 ns | 70.05 ns | 65.53 ns | 9.01 KB |
| FakeItEasy | 5,505.9 ns | 108.67 ns | 120.79 ns | 10.44 KB |

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
  y-axis "Time (ns)" 0 --> 226337
  bar [532.4, 683.9, 301.6, 188613.6, 5683.3, 5505.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 794.2 ns | 11.82 ns | 11.06 ns | 3.15 KB |
| Imposter | 1,238.8 ns | 24.65 ns | 32.05 ns | 10.59 KB |
| Mockolate | 532.8 ns | 10.67 ns | 11.42 ns | 2.35 KB |
| Moq | 52,347.2 ns | 175.87 ns | 155.91 ns | 16.52 KB |
| NSubstitute | 9,298.8 ns | 109.32 ns | 102.26 ns | 20.31 KB |
| FakeItEasy | 5,422.6 ns | 106.62 ns | 122.78 ns | 11.78 KB |

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
  y-axis "Time (ns)" 0 --> 62817
  bar [794.2, 1238.8, 532.8, 52347.2, 9298.8, 5422.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-11T02:38:48.126Z*
