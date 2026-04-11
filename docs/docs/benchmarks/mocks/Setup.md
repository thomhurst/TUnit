---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 567.5 ns | 8.12 ns | 7.60 ns | 2.34 KB |
| Imposter | 794.7 ns | 15.77 ns | 21.59 ns | 6.12 KB |
| Mockolate | 440.2 ns | 7.25 ns | 6.05 ns | 2.03 KB |
| Moq | 424,301.1 ns | 2,711.50 ns | 2,403.68 ns | 28.63 KB |
| NSubstitute | 5,638.0 ns | 43.16 ns | 36.04 ns | 9.01 KB |
| FakeItEasy | 7,808.2 ns | 126.54 ns | 118.37 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 509162
  bar [567.5, 794.7, 440.2, 424301.1, 5638, 7808.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 709.7 ns | 14.03 ns | 17.23 ns | 2.93 KB |
| Imposter | 1,346.3 ns | 26.79 ns | 45.48 ns | 10.59 KB |
| Mockolate | 660.3 ns | 3.27 ns | 3.06 ns | 3.07 KB |
| Moq | 112,421.9 ns | 805.62 ns | 672.73 ns | 16.53 KB |
| NSubstitute | 11,787.2 ns | 25.04 ns | 22.19 ns | 20.31 KB |
| FakeItEasy | 7,300.9 ns | 77.58 ns | 64.79 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134907
  bar [709.7, 1346.3, 660.3, 112421.9, 11787.2, 7300.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-11T03:20:45.459Z*
