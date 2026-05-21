---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 556.5 ns | 11.21 ns | 19.03 ns | 2.31 KB |
| Imposter | 932.1 ns | 15.41 ns | 13.66 ns | 6.12 KB |
| Mockolate | 340.8 ns | 6.87 ns | 10.07 ns | 1.65 KB |
| Moq | 424,605.2 ns | 1,636.58 ns | 1,366.62 ns | 28.52 KB |
| NSubstitute | 5,618.9 ns | 19.78 ns | 16.52 ns | 9.01 KB |
| FakeItEasy | 8,237.1 ns | 161.50 ns | 151.07 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 509527
  bar [556.5, 932.1, 340.8, 424605.2, 5618.9, 8237.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 808.3 ns | 16.14 ns | 20.41 ns | 3.09 KB |
| Imposter | 1,376.7 ns | 27.55 ns | 36.78 ns | 10.59 KB |
| Mockolate | 557.6 ns | 10.24 ns | 10.06 ns | 2.6 KB |
| Moq | 114,269.0 ns | 639.68 ns | 567.06 ns | 16.53 KB |
| NSubstitute | 12,098.6 ns | 127.67 ns | 119.43 ns | 20.31 KB |
| FakeItEasy | 7,797.7 ns | 73.11 ns | 64.81 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 137123
  bar [808.3, 1376.7, 557.6, 114269, 12098.6, 7797.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-21T03:28:27.059Z*
