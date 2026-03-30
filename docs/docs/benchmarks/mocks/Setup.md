---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 572.4 ns | 4.46 ns | 4.17 ns | 2.15 KB |
| Imposter | 792.4 ns | 8.74 ns | 8.17 ns | 6.12 KB |
| Mockolate | 397.9 ns | 5.02 ns | 4.45 ns | 2.04 KB |
| Moq | 414,953.7 ns | 1,325.58 ns | 1,239.95 ns | 28.52 KB |
| NSubstitute | 5,364.6 ns | 38.43 ns | 34.07 ns | 9.01 KB |
| FakeItEasy | 7,917.3 ns | 71.62 ns | 66.99 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 497945
  bar [572.4, 792.4, 397.9, 414953.7, 5364.6, 7917.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 828.7 ns | 4.87 ns | 4.55 ns | 2.98 KB |
| Imposter | 1,324.3 ns | 17.84 ns | 15.82 ns | 10.59 KB |
| Mockolate | 602.4 ns | 11.96 ns | 16.76 ns | 3.05 KB |
| Moq | 112,561.8 ns | 692.09 ns | 647.38 ns | 16.64 KB |
| NSubstitute | 12,472.1 ns | 131.73 ns | 116.78 ns | 20.31 KB |
| FakeItEasy | 7,836.2 ns | 82.65 ns | 73.27 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 135075
  bar [828.7, 1324.3, 602.4, 112561.8, 12472.1, 7836.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T03:24:56.545Z*
