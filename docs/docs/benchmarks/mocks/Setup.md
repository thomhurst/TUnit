---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 508.9 ns | 3.96 ns | 3.51 ns | 2.34 KB |
| Imposter | 765.6 ns | 3.02 ns | 2.67 ns | 6.12 KB |
| Mockolate | 305.0 ns | 2.48 ns | 2.32 ns | 1.41 KB |
| Moq | 426,507.3 ns | 1,981.28 ns | 1,654.46 ns | 28.63 KB |
| NSubstitute | 5,507.0 ns | 14.09 ns | 11.76 ns | 9.01 KB |
| FakeItEasy | 8,331.1 ns | 52.73 ns | 46.75 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 511809
  bar [508.9, 765.6, 305, 426507.3, 5507, 8331.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 732.0 ns | 3.27 ns | 3.06 ns | 3.15 KB |
| Imposter | 1,349.3 ns | 8.22 ns | 7.69 ns | 10.59 KB |
| Mockolate | 518.9 ns | 2.00 ns | 1.67 ns | 2.35 KB |
| Moq | 114,450.2 ns | 1,035.90 ns | 918.30 ns | 16.53 KB |
| NSubstitute | 11,770.2 ns | 131.93 ns | 123.41 ns | 20.31 KB |
| FakeItEasy | 7,865.7 ns | 108.96 ns | 101.93 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 137341
  bar [732, 1349.3, 518.9, 114450.2, 11770.2, 7865.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-20T03:22:58.159Z*
