---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 540.2 ns | 4.95 ns | 4.63 ns | 2.31 KB |
| Imposter | 757.1 ns | 6.71 ns | 5.95 ns | 6.12 KB |
| Mockolate | 359.1 ns | 2.25 ns | 1.99 ns | 1.65 KB |
| Moq | 298,446.7 ns | 1,554.11 ns | 1,213.35 ns | 28.52 KB |
| NSubstitute | 5,228.7 ns | 54.83 ns | 51.29 ns | 9.06 KB |
| FakeItEasy | 7,010.2 ns | 47.72 ns | 39.85 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 358137
  bar [540.2, 757.1, 359.1, 298446.7, 5228.7, 7010.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 783.3 ns | 6.99 ns | 6.19 ns | 3.09 KB |
| Imposter | 1,406.8 ns | 20.76 ns | 18.40 ns | 10.59 KB |
| Mockolate | 577.8 ns | 3.66 ns | 3.06 ns | 2.6 KB |
| Moq | 88,527.5 ns | 1,290.13 ns | 1,206.79 ns | 16.53 KB |
| NSubstitute | 10,702.7 ns | 64.66 ns | 57.32 ns | 20.31 KB |
| FakeItEasy | 6,901.6 ns | 112.81 ns | 105.52 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 106233
  bar [783.3, 1406.8, 577.8, 88527.5, 10702.7, 6901.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-24T03:32:03.972Z*
