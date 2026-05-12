---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 415.6 ns | 1.21 ns | 1.13 ns | 2.01 KB |
| Imposter | 780.7 ns | 2.07 ns | 1.83 ns | 6.12 KB |
| Mockolate | 331.7 ns | 2.31 ns | 2.05 ns | 1.65 KB |
| Moq | 417,430.0 ns | 1,472.03 ns | 1,304.92 ns | 28.52 KB |
| NSubstitute | 5,338.7 ns | 25.22 ns | 22.35 ns | 9.06 KB |
| FakeItEasy | 7,984.6 ns | 32.72 ns | 29.01 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 500916
  bar [415.6, 780.7, 331.7, 417430, 5338.7, 7984.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 585.7 ns | 1.14 ns | 1.01 ns | 2.59 KB |
| Imposter | 1,427.6 ns | 3.72 ns | 3.30 ns | 10.59 KB |
| Mockolate | 546.8 ns | 8.07 ns | 7.55 ns | 2.6 KB |
| Moq | 114,742.7 ns | 861.64 ns | 805.98 ns | 16.53 KB |
| NSubstitute | 11,512.2 ns | 61.12 ns | 47.72 ns | 20.31 KB |
| FakeItEasy | 7,952.6 ns | 71.80 ns | 63.65 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 137692
  bar [585.7, 1427.6, 546.8, 114742.7, 11512.2, 7952.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-12T03:27:02.666Z*
