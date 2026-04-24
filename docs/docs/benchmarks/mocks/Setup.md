---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 442.6 ns | 8.57 ns | 8.42 ns | 2.01 KB |
| Imposter | 816.7 ns | 16.31 ns | 16.75 ns | 6.12 KB |
| Mockolate | 451.6 ns | 4.14 ns | 3.45 ns | 2.03 KB |
| Moq | 424,533.3 ns | 1,123.62 ns | 1,051.03 ns | 28.66 KB |
| NSubstitute | 5,580.8 ns | 50.44 ns | 44.72 ns | 9.01 KB |
| FakeItEasy | 8,265.3 ns | 36.71 ns | 30.65 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 509440
  bar [442.6, 816.7, 451.6, 424533.3, 5580.8, 8265.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 619.5 ns | 4.35 ns | 4.07 ns | 2.59 KB |
| Imposter | 1,414.7 ns | 23.41 ns | 19.55 ns | 10.59 KB |
| Mockolate | 737.0 ns | 13.81 ns | 12.92 ns | 3.07 KB |
| Moq | 114,379.8 ns | 526.21 ns | 466.47 ns | 16.53 KB |
| NSubstitute | 12,181.6 ns | 190.13 ns | 168.55 ns | 20.5 KB |
| FakeItEasy | 7,822.8 ns | 145.90 ns | 129.33 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 137256
  bar [619.5, 1414.7, 737, 114379.8, 12181.6, 7822.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-24T03:24:24.137Z*
