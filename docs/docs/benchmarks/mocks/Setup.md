---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 529.7 ns | 3.15 ns | 2.94 ns | 2.34 KB |
| Imposter | 780.9 ns | 7.39 ns | 6.91 ns | 6.12 KB |
| Mockolate | 439.2 ns | 3.20 ns | 2.67 ns | 2.03 KB |
| Moq | 428,969.5 ns | 1,383.72 ns | 1,226.63 ns | 28.52 KB |
| NSubstitute | 5,584.0 ns | 84.91 ns | 75.27 ns | 9.01 KB |
| FakeItEasy | 8,154.5 ns | 67.86 ns | 63.47 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 514764
  bar [529.7, 780.9, 439.2, 428969.5, 5584, 8154.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 697.6 ns | 13.26 ns | 11.76 ns | 2.93 KB |
| Imposter | 1,383.5 ns | 25.46 ns | 23.81 ns | 10.59 KB |
| Mockolate | 707.2 ns | 11.31 ns | 10.58 ns | 3.07 KB |
| Moq | 113,374.7 ns | 371.58 ns | 329.40 ns | 16.64 KB |
| NSubstitute | 12,310.7 ns | 97.70 ns | 86.61 ns | 20.31 KB |
| FakeItEasy | 7,864.8 ns | 132.44 ns | 110.59 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136050
  bar [697.6, 1383.5, 707.2, 113374.7, 12310.7, 7864.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-20T03:23:48.728Z*
