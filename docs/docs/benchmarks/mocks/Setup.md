---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 478.4 ns | 5.27 ns | 4.93 ns | 2.01 KB |
| Imposter | 889.8 ns | 10.69 ns | 10.00 ns | 6.12 KB |
| Mockolate | 374.7 ns | 2.54 ns | 2.37 ns | 1.65 KB |
| Moq | 304,474.6 ns | 3,966.89 ns | 3,516.55 ns | 28.52 KB |
| NSubstitute | 5,426.6 ns | 20.09 ns | 18.79 ns | 9.01 KB |
| FakeItEasy | 7,549.8 ns | 57.24 ns | 50.74 ns | 10.56 KB |

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
  y-axis "Time (ns)" 0 --> 365370
  bar [478.4, 889.8, 374.7, 304474.6, 5426.6, 7549.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 674.3 ns | 5.89 ns | 5.51 ns | 2.59 KB |
| Imposter | 1,580.0 ns | 19.93 ns | 18.64 ns | 10.59 KB |
| Mockolate | 635.7 ns | 4.40 ns | 4.12 ns | 2.6 KB |
| Moq | 87,700.7 ns | 868.89 ns | 770.25 ns | 16.53 KB |
| NSubstitute | 11,565.6 ns | 43.29 ns | 36.15 ns | 20.31 KB |
| FakeItEasy | 7,411.6 ns | 51.17 ns | 42.73 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 105241
  bar [674.3, 1580, 635.7, 87700.7, 11565.6, 7411.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-13T03:26:48.570Z*
