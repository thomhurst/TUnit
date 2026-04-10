---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 546.2 ns | 8.18 ns | 7.65 ns | 2.34 KB |
| Imposter | 802.1 ns | 13.63 ns | 24.24 ns | 6.12 KB |
| Mockolate | 435.2 ns | 3.99 ns | 3.33 ns | 2.03 KB |
| Moq | 428,031.2 ns | 1,898.48 ns | 1,775.84 ns | 28.52 KB |
| NSubstitute | 5,744.8 ns | 84.90 ns | 70.89 ns | 9.01 KB |
| FakeItEasy | 8,043.5 ns | 101.37 ns | 94.82 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 513638
  bar [546.2, 802.1, 435.2, 428031.2, 5744.8, 8043.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 721.1 ns | 8.19 ns | 7.66 ns | 2.93 KB |
| Imposter | 1,371.2 ns | 14.54 ns | 12.89 ns | 10.59 KB |
| Mockolate | 703.3 ns | 6.88 ns | 6.43 ns | 3.07 KB |
| Moq | 115,656.1 ns | 546.79 ns | 511.47 ns | 16.53 KB |
| NSubstitute | 11,725.3 ns | 117.66 ns | 110.06 ns | 20.31 KB |
| FakeItEasy | 7,707.6 ns | 148.42 ns | 145.77 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 138788
  bar [721.1, 1371.2, 703.3, 115656.1, 11725.3, 7707.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-10T03:23:10.636Z*
