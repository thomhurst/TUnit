---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 514.9 ns | 2.68 ns | 2.37 ns | 2.34 KB |
| Imposter | 797.9 ns | 12.11 ns | 11.33 ns | 6.12 KB |
| Mockolate | 313.6 ns | 3.00 ns | 2.66 ns | 1.41 KB |
| Moq | 429,572.3 ns | 2,393.52 ns | 2,238.90 ns | 28.52 KB |
| NSubstitute | 5,535.1 ns | 51.22 ns | 47.91 ns | 9.01 KB |
| FakeItEasy | 8,102.7 ns | 27.55 ns | 24.43 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 515487
  bar [514.9, 797.9, 313.6, 429572.3, 5535.1, 8102.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 770.6 ns | 4.11 ns | 3.84 ns | 3.15 KB |
| Imposter | 1,332.2 ns | 11.56 ns | 10.81 ns | 10.59 KB |
| Mockolate | 535.2 ns | 3.20 ns | 2.84 ns | 2.35 KB |
| Moq | 115,441.8 ns | 355.50 ns | 315.14 ns | 16.53 KB |
| NSubstitute | 11,937.1 ns | 146.51 ns | 129.88 ns | 20.31 KB |
| FakeItEasy | 7,941.3 ns | 98.00 ns | 91.67 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 138531
  bar [770.6, 1332.2, 535.2, 115441.8, 11937.1, 7941.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-04T03:22:20.303Z*
