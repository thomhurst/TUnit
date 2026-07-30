---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 504.9 ns | 10.06 ns | 14.10 ns | 2.34 KB |
| Imposter | 645.3 ns | 7.87 ns | 6.97 ns | 6.12 KB |
| Mockolate | 298.6 ns | 5.86 ns | 5.76 ns | 1.41 KB |
| Moq | 186,005.1 ns | 1,258.06 ns | 1,115.24 ns | 28.46 KB |
| NSubstitute | 5,162.7 ns | 77.59 ns | 68.78 ns | 9.01 KB |
| FakeItEasy | 5,418.3 ns | 101.63 ns | 90.10 ns | 10.55 KB |

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
  y-axis "Time (ns)" 0 --> 223207
  bar [504.9, 645.3, 298.6, 186005.1, 5162.7, 5418.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.4 ns | 12.25 ns | 11.46 ns | 3.15 KB |
| Imposter | 1,194.2 ns | 19.99 ns | 18.70 ns | 10.59 KB |
| Mockolate | 499.5 ns | 10.05 ns | 11.17 ns | 2.35 KB |
| Moq | 50,201.0 ns | 362.20 ns | 338.80 ns | 16.52 KB |
| NSubstitute | 8,614.9 ns | 156.51 ns | 138.74 ns | 20.31 KB |
| FakeItEasy | 5,203.3 ns | 84.79 ns | 75.16 ns | 11.78 KB |

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
  y-axis "Time (ns)" 0 --> 60242
  bar [740.4, 1194.2, 499.5, 50201, 8614.9, 5203.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-30T03:21:07.533Z*
