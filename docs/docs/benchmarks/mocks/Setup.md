---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 600.5 ns | 8.62 ns | 8.06 ns | 2.34 KB |
| Imposter | 862.0 ns | 17.14 ns | 18.34 ns | 6.12 KB |
| Mockolate | 373.3 ns | 4.74 ns | 4.43 ns | 1.65 KB |
| Moq | 325,796.5 ns | 2,311.24 ns | 2,161.94 ns | 28.67 KB |
| NSubstitute | 5,540.7 ns | 28.45 ns | 25.22 ns | 9.01 KB |
| FakeItEasy | 7,847.2 ns | 42.33 ns | 39.59 ns | 10.46 KB |

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
  y-axis "Time (ns)" 0 --> 390956
  bar [600.5, 862, 373.3, 325796.5, 5540.7, 7847.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 913.9 ns | 10.96 ns | 10.25 ns | 3.14 KB |
| Imposter | 1,501.3 ns | 29.22 ns | 28.70 ns | 10.59 KB |
| Mockolate | 601.7 ns | 11.94 ns | 12.26 ns | 2.6 KB |
| Moq | 85,739.4 ns | 1,174.93 ns | 1,099.03 ns | 16.53 KB |
| NSubstitute | 11,410.4 ns | 109.48 ns | 102.41 ns | 20.31 KB |
| FakeItEasy | 7,198.8 ns | 32.47 ns | 30.37 ns | 11.72 KB |

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
  y-axis "Time (ns)" 0 --> 102888
  bar [913.9, 1501.3, 601.7, 85739.4, 11410.4, 7198.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-10T03:28:13.506Z*
