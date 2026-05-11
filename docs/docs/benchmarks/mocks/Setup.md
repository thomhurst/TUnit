---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 419.2 ns | 3.95 ns | 3.70 ns | 2.01 KB |
| Imposter | 797.4 ns | 15.94 ns | 15.65 ns | 6.12 KB |
| Mockolate | 348.4 ns | 2.62 ns | 2.32 ns | 1.68 KB |
| Moq | 431,677.7 ns | 2,089.53 ns | 1,852.32 ns | 28.52 KB |
| NSubstitute | 5,403.2 ns | 103.70 ns | 97.00 ns | 9.01 KB |
| FakeItEasy | 8,126.2 ns | 51.08 ns | 45.28 ns | 10.53 KB |

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
  y-axis "Time (ns)" 0 --> 518014
  bar [419.2, 797.4, 348.4, 431677.7, 5403.2, 8126.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 603.6 ns | 9.30 ns | 8.70 ns | 2.59 KB |
| Imposter | 1,349.1 ns | 3.05 ns | 2.54 ns | 10.59 KB |
| Mockolate | 607.9 ns | 4.49 ns | 3.75 ns | 2.82 KB |
| Moq | 112,072.3 ns | 990.14 ns | 926.18 ns | 16.53 KB |
| NSubstitute | 11,610.9 ns | 139.14 ns | 130.15 ns | 20.31 KB |
| FakeItEasy | 7,661.8 ns | 76.36 ns | 71.42 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134487
  bar [603.6, 1349.1, 607.9, 112072.3, 11610.9, 7661.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-11T03:29:06.162Z*
