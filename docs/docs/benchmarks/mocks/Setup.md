---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 573.8 ns | 8.69 ns | 8.13 ns | 2.34 KB |
| Imposter | 874.8 ns | 17.45 ns | 42.14 ns | 6.12 KB |
| Mockolate | 334.9 ns | 4.36 ns | 3.64 ns | 1.41 KB |
| Moq | 443,003.9 ns | 1,907.15 ns | 1,690.64 ns | 28.68 KB |
| NSubstitute | 5,644.4 ns | 68.69 ns | 64.25 ns | 9.01 KB |
| FakeItEasy | 8,639.6 ns | 125.67 ns | 117.55 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 531605
  bar [573.8, 874.8, 334.9, 443003.9, 5644.4, 8639.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 806.3 ns | 15.80 ns | 18.20 ns | 3.15 KB |
| Imposter | 1,403.9 ns | 19.96 ns | 18.67 ns | 10.59 KB |
| Mockolate | 548.4 ns | 10.43 ns | 9.75 ns | 2.35 KB |
| Moq | 116,683.1 ns | 1,155.80 ns | 1,024.59 ns | 16.67 KB |
| NSubstitute | 12,180.8 ns | 134.89 ns | 112.64 ns | 20.31 KB |
| FakeItEasy | 8,106.1 ns | 101.92 ns | 95.33 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 140020
  bar [806.3, 1403.9, 548.4, 116683.1, 12180.8, 8106.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-28T03:33:50.965Z*
