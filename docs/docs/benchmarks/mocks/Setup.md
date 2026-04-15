---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 522.5 ns | 2.78 ns | 2.60 ns | 2.34 KB |
| Imposter | 802.5 ns | 9.24 ns | 8.20 ns | 6.12 KB |
| Mockolate | 449.4 ns | 6.19 ns | 5.79 ns | 2.03 KB |
| Moq | 425,063.5 ns | 1,808.02 ns | 1,691.23 ns | 28.66 KB |
| NSubstitute | 5,474.1 ns | 48.17 ns | 45.06 ns | 9.01 KB |
| FakeItEasy | 8,065.0 ns | 49.41 ns | 43.80 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 510077
  bar [522.5, 802.5, 449.4, 425063.5, 5474.1, 8065]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 723.5 ns | 5.60 ns | 4.67 ns | 2.93 KB |
| Imposter | 1,356.0 ns | 7.22 ns | 6.40 ns | 10.59 KB |
| Mockolate | 706.8 ns | 7.66 ns | 6.40 ns | 3.07 KB |
| Moq | 117,008.6 ns | 661.05 ns | 586.01 ns | 16.75 KB |
| NSubstitute | 12,373.9 ns | 190.77 ns | 159.30 ns | 20.5 KB |
| FakeItEasy | 7,871.3 ns | 117.34 ns | 104.02 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 140411
  bar [723.5, 1356, 706.8, 117008.6, 12373.9, 7871.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-15T03:22:40.574Z*
