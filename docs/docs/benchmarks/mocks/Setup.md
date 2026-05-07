---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 442.5 ns | 7.28 ns | 6.81 ns | 2.01 KB |
| Imposter | 911.3 ns | 18.19 ns | 29.89 ns | 6.12 KB |
| Mockolate | 364.2 ns | 7.16 ns | 7.95 ns | 1.68 KB |
| Moq | 431,403.0 ns | 2,092.55 ns | 1,854.99 ns | 28.64 KB |
| NSubstitute | 5,626.7 ns | 34.76 ns | 29.02 ns | 9.06 KB |
| FakeItEasy | 8,246.7 ns | 88.84 ns | 83.10 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 517684
  bar [442.5, 911.3, 364.2, 431403, 5626.7, 8246.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 636.4 ns | 12.75 ns | 13.10 ns | 2.59 KB |
| Imposter | 1,409.7 ns | 16.41 ns | 13.71 ns | 10.59 KB |
| Mockolate | 644.7 ns | 12.93 ns | 15.40 ns | 2.82 KB |
| Moq | 114,127.6 ns | 523.62 ns | 489.79 ns | 16.53 KB |
| NSubstitute | 12,312.4 ns | 125.64 ns | 111.37 ns | 20.31 KB |
| FakeItEasy | 8,144.3 ns | 151.36 ns | 134.18 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136954
  bar [636.4, 1409.7, 644.7, 114127.6, 12312.4, 8144.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-07T03:27:11.074Z*
