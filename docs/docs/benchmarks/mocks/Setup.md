---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 564.8 ns | 2.52 ns | 2.36 ns | 2.34 KB |
| Imposter | 798.2 ns | 11.44 ns | 10.14 ns | 6.12 KB |
| Mockolate | 477.8 ns | 2.58 ns | 2.41 ns | 2.03 KB |
| Moq | 294,686.5 ns | 1,918.22 ns | 1,794.30 ns | 28.52 KB |
| NSubstitute | 5,278.8 ns | 48.77 ns | 45.61 ns | 9.01 KB |
| FakeItEasy | 7,246.1 ns | 97.84 ns | 91.52 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 353624
  bar [564.8, 798.2, 477.8, 294686.5, 5278.8, 7246.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 787.9 ns | 7.43 ns | 6.58 ns | 2.93 KB |
| Imposter | 1,338.9 ns | 4.62 ns | 4.10 ns | 10.59 KB |
| Mockolate | 748.6 ns | 3.94 ns | 3.68 ns | 3.07 KB |
| Moq | 89,404.6 ns | 875.09 ns | 730.74 ns | 16.53 KB |
| NSubstitute | 11,169.5 ns | 106.88 ns | 99.98 ns | 20.5 KB |
| FakeItEasy | 7,008.5 ns | 101.02 ns | 94.49 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 107286
  bar [787.9, 1338.9, 748.6, 89404.6, 11169.5, 7008.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-23T03:25:34.373Z*
