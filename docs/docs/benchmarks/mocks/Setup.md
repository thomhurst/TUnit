---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 520.2 ns | 2.24 ns | 2.09 ns | 2.31 KB |
| Imposter | 865.1 ns | 6.43 ns | 5.70 ns | 6.12 KB |
| Mockolate | 326.4 ns | 2.52 ns | 2.36 ns | 1.65 KB |
| Moq | 418,410.7 ns | 2,660.08 ns | 2,488.24 ns | 28.64 KB |
| NSubstitute | 5,435.2 ns | 21.30 ns | 19.92 ns | 9.06 KB |
| FakeItEasy | 7,960.2 ns | 53.69 ns | 50.22 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 502093
  bar [520.2, 865.1, 326.4, 418410.7, 5435.2, 7960.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 739.9 ns | 6.18 ns | 5.78 ns | 3.09 KB |
| Imposter | 1,281.8 ns | 11.89 ns | 11.12 ns | 10.59 KB |
| Mockolate | 548.4 ns | 4.13 ns | 3.86 ns | 2.6 KB |
| Moq | 112,257.9 ns | 817.46 ns | 724.65 ns | 16.53 KB |
| NSubstitute | 12,039.0 ns | 141.89 ns | 125.78 ns | 20.5 KB |
| FakeItEasy | 7,883.2 ns | 96.07 ns | 80.22 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134710
  bar [739.9, 1281.8, 548.4, 112257.9, 12039, 7883.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-31T03:32:45.264Z*
