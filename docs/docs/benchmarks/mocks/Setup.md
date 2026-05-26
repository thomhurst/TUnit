---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 418.1 ns | 3.75 ns | 3.32 ns | 2.31 KB |
| Imposter | 618.4 ns | 6.93 ns | 6.48 ns | 6.12 KB |
| Mockolate | 286.5 ns | 3.92 ns | 3.48 ns | 1.65 KB |
| Moq | 244,933.5 ns | 1,749.99 ns | 1,636.94 ns | 28.7 KB |
| NSubstitute | 4,016.2 ns | 28.05 ns | 26.23 ns | 9.06 KB |
| FakeItEasy | 5,476.4 ns | 42.30 ns | 39.57 ns | 10.6 KB |

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
  y-axis "Time (ns)" 0 --> 293921
  bar [418.1, 618.4, 286.5, 244933.5, 4016.2, 5476.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 601.8 ns | 8.24 ns | 7.71 ns | 3.09 KB |
| Imposter | 1,042.5 ns | 3.99 ns | 3.53 ns | 10.59 KB |
| Mockolate | 455.1 ns | 1.55 ns | 1.45 ns | 2.6 KB |
| Moq | 67,933.8 ns | 484.58 ns | 429.57 ns | 16.53 KB |
| NSubstitute | 8,199.0 ns | 68.48 ns | 64.06 ns | 20.31 KB |
| FakeItEasy | 5,125.3 ns | 36.29 ns | 30.30 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 81521
  bar [601.8, 1042.5, 455.1, 67933.8, 8199, 5125.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-26T03:27:58.119Z*
