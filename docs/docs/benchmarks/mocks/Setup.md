---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 511.3 ns | 2.48 ns | 2.07 ns | 2.34 KB |
| Imposter | 811.3 ns | 3.58 ns | 3.17 ns | 6.12 KB |
| Mockolate | 303.7 ns | 1.66 ns | 1.47 ns | 1.41 KB |
| Moq | 428,461.6 ns | 1,086.08 ns | 962.78 ns | 28.52 KB |
| NSubstitute | 5,494.5 ns | 44.89 ns | 39.79 ns | 9.01 KB |
| FakeItEasy | 8,294.5 ns | 56.02 ns | 46.78 ns | 10.6 KB |

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
  y-axis "Time (ns)" 0 --> 514154
  bar [511.3, 811.3, 303.7, 428461.6, 5494.5, 8294.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 732.3 ns | 2.69 ns | 2.24 ns | 3.15 KB |
| Imposter | 1,328.5 ns | 3.51 ns | 2.93 ns | 10.59 KB |
| Mockolate | 535.3 ns | 2.64 ns | 2.34 ns | 2.35 KB |
| Moq | 115,696.4 ns | 681.70 ns | 637.66 ns | 16.64 KB |
| NSubstitute | 11,817.5 ns | 115.27 ns | 107.83 ns | 20.31 KB |
| FakeItEasy | 7,987.4 ns | 80.03 ns | 70.95 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 138836
  bar [732.3, 1328.5, 535.3, 115696.4, 11817.5, 7987.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-29T03:20:13.661Z*
