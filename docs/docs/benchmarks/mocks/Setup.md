---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 421.5 ns | 7.07 ns | 5.90 ns | 2.34 KB |
| Imposter | 665.9 ns | 13.34 ns | 25.05 ns | 6.12 KB |
| Mockolate | 255.6 ns | 4.02 ns | 3.36 ns | 1.41 KB |
| Moq | 159,727.2 ns | 2,598.80 ns | 2,552.37 ns | 28.61 KB |
| NSubstitute | 4,769.0 ns | 93.76 ns | 87.71 ns | 9.01 KB |
| FakeItEasy | 4,569.9 ns | 88.62 ns | 118.30 ns | 10.44 KB |

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
  y-axis "Time (ns)" 0 --> 191673
  bar [421.5, 665.9, 255.6, 159727.2, 4769, 4569.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 674.2 ns | 12.98 ns | 16.41 ns | 3.15 KB |
| Imposter | 1,087.0 ns | 21.54 ns | 28.75 ns | 10.59 KB |
| Mockolate | 440.9 ns | 6.52 ns | 5.78 ns | 2.35 KB |
| Moq | 42,070.2 ns | 453.37 ns | 378.58 ns | 16.52 KB |
| NSubstitute | 8,155.3 ns | 161.65 ns | 315.28 ns | 20.66 KB |
| FakeItEasy | 4,233.0 ns | 83.31 ns | 129.70 ns | 11.7 KB |

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
  y-axis "Time (ns)" 0 --> 50485
  bar [674.2, 1087, 440.9, 42070.2, 8155.3, 4233]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-04T02:33:16.366Z*
