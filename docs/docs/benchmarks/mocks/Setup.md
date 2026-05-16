---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 461.8 ns | 5.59 ns | 5.23 ns | 2.01 KB |
| Imposter | 779.7 ns | 6.41 ns | 6.00 ns | 6.12 KB |
| Mockolate | 371.5 ns | 7.25 ns | 7.12 ns | 1.65 KB |
| Moq | 308,367.5 ns | 2,542.44 ns | 2,378.20 ns | 28.52 KB |
| NSubstitute | 5,358.1 ns | 68.52 ns | 60.74 ns | 9.01 KB |
| FakeItEasy | 7,477.7 ns | 24.37 ns | 20.35 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 370041
  bar [461.8, 779.7, 371.5, 308367.5, 5358.1, 7477.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 679.4 ns | 13.24 ns | 15.25 ns | 2.59 KB |
| Imposter | 1,372.1 ns | 12.32 ns | 13.18 ns | 10.59 KB |
| Mockolate | 580.3 ns | 5.81 ns | 4.85 ns | 2.6 KB |
| Moq | 88,188.2 ns | 659.42 ns | 584.56 ns | 16.53 KB |
| NSubstitute | 10,763.0 ns | 166.02 ns | 155.29 ns | 20.31 KB |
| FakeItEasy | 6,777.1 ns | 62.18 ns | 55.12 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 105826
  bar [679.4, 1372.1, 580.3, 88188.2, 10763, 6777.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-16T03:25:52.400Z*
