---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 527.2 ns | 6.53 ns | 6.10 ns | 2.03 KB |
| Imposter | 780.1 ns | 15.27 ns | 20.39 ns | 6.12 KB |
| Mockolate | 447.5 ns | 4.73 ns | 4.19 ns | 2.03 KB |
| Moq | 432,690.5 ns | 2,346.45 ns | 2,080.06 ns | 28.74 KB |
| NSubstitute | 5,393.7 ns | 38.17 ns | 35.71 ns | 9.01 KB |
| FakeItEasy | 8,158.3 ns | 61.47 ns | 57.50 ns | 10.53 KB |

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
  y-axis "Time (ns)" 0 --> 519229
  bar [527.2, 780.1, 447.5, 432690.5, 5393.7, 8158.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 725.2 ns | 6.19 ns | 5.17 ns | 2.7 KB |
| Imposter | 1,359.1 ns | 19.62 ns | 18.35 ns | 10.59 KB |
| Mockolate | 694.8 ns | 5.29 ns | 4.42 ns | 3.07 KB |
| Moq | 113,611.1 ns | 1,690.58 ns | 1,581.37 ns | 16.53 KB |
| NSubstitute | 12,279.3 ns | 115.54 ns | 102.42 ns | 20.31 KB |
| FakeItEasy | 7,964.4 ns | 100.19 ns | 93.72 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 136334
  bar [725.2, 1359.1, 694.8, 113611.1, 12279.3, 7964.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T03:22:20.916Z*
