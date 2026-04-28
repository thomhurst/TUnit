---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 421.8 ns | 0.71 ns | 0.59 ns | 2.01 KB |
| Imposter | 761.6 ns | 2.07 ns | 1.61 ns | 6.12 KB |
| Mockolate | 447.6 ns | 7.26 ns | 6.79 ns | 2.03 KB |
| Moq | 421,994.9 ns | 2,118.19 ns | 1,768.79 ns | 28.63 KB |
| NSubstitute | 5,419.8 ns | 11.78 ns | 9.84 ns | 9.06 KB |
| FakeItEasy | 8,191.7 ns | 21.55 ns | 19.10 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 506394
  bar [421.8, 761.6, 447.6, 421994.9, 5419.8, 8191.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 586.1 ns | 1.63 ns | 1.52 ns | 2.59 KB |
| Imposter | 1,339.0 ns | 4.13 ns | 3.86 ns | 10.59 KB |
| Mockolate | 706.7 ns | 13.91 ns | 14.88 ns | 3.07 KB |
| Moq | 112,925.5 ns | 444.00 ns | 370.76 ns | 16.53 KB |
| NSubstitute | 11,909.3 ns | 42.32 ns | 37.51 ns | 20.47 KB |
| FakeItEasy | 7,678.7 ns | 81.33 ns | 67.91 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 135511
  bar [586.1, 1339, 706.7, 112925.5, 11909.3, 7678.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-28T03:25:54.642Z*
