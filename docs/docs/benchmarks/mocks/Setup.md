---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 527.8 ns | 10.09 ns | 9.44 ns | 2.34 KB |
| Imposter | 804.3 ns | 15.84 ns | 14.82 ns | 6.12 KB |
| Mockolate | 308.4 ns | 2.97 ns | 2.63 ns | 1.41 KB |
| Moq | 426,517.8 ns | 2,911.36 ns | 2,723.29 ns | 28.63 KB |
| NSubstitute | 5,571.5 ns | 49.81 ns | 46.59 ns | 9.01 KB |
| FakeItEasy | 8,341.9 ns | 62.71 ns | 58.66 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 511822
  bar [527.8, 804.3, 308.4, 426517.8, 5571.5, 8341.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 820.2 ns | 7.45 ns | 6.60 ns | 3.15 KB |
| Imposter | 1,384.1 ns | 17.06 ns | 15.96 ns | 10.59 KB |
| Mockolate | 565.6 ns | 3.22 ns | 2.51 ns | 2.35 KB |
| Moq | 115,914.6 ns | 611.02 ns | 541.65 ns | 16.53 KB |
| NSubstitute | 12,231.5 ns | 84.55 ns | 74.95 ns | 20.31 KB |
| FakeItEasy | 7,889.1 ns | 101.98 ns | 90.40 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 139098
  bar [820.2, 1384.1, 565.6, 115914.6, 12231.5, 7889.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-09T03:24:10.827Z*
