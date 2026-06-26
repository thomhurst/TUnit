---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 506.7 ns | 4.58 ns | 4.06 ns | 2.34 KB |
| Imposter | 827.1 ns | 23.52 ns | 69.36 ns | 6.12 KB |
| Mockolate | 348.1 ns | 3.06 ns | 2.71 ns | 1.41 KB |
| Moq | 426,638.2 ns | 2,135.46 ns | 1,893.03 ns | 28.52 KB |
| NSubstitute | 5,691.2 ns | 29.37 ns | 26.03 ns | 9.01 KB |
| FakeItEasy | 7,858.5 ns | 41.26 ns | 36.57 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 511966
  bar [506.7, 827.1, 348.1, 426638.2, 5691.2, 7858.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 759.4 ns | 5.77 ns | 5.12 ns | 3.15 KB |
| Imposter | 1,323.7 ns | 10.98 ns | 9.73 ns | 10.59 KB |
| Mockolate | 533.6 ns | 3.28 ns | 2.90 ns | 2.35 KB |
| Moq | 116,513.8 ns | 431.03 ns | 382.10 ns | 16.64 KB |
| NSubstitute | 11,713.4 ns | 71.75 ns | 59.91 ns | 20.31 KB |
| FakeItEasy | 8,107.0 ns | 110.67 ns | 103.52 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 139817
  bar [759.4, 1323.7, 533.6, 116513.8, 11713.4, 8107]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-26T03:28:53.126Z*
