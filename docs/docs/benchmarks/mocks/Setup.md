---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 557.4 ns | 8.70 ns | 8.14 ns | 2.34 KB |
| Imposter | 763.7 ns | 15.16 ns | 28.48 ns | 6.12 KB |
| Mockolate | 437.8 ns | 2.22 ns | 1.85 ns | 2.03 KB |
| Moq | 418,995.5 ns | 2,484.70 ns | 2,324.19 ns | 28.52 KB |
| NSubstitute | 5,422.0 ns | 42.62 ns | 39.86 ns | 9.01 KB |
| FakeItEasy | 8,015.8 ns | 72.55 ns | 67.86 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 502795
  bar [557.4, 763.7, 437.8, 418995.5, 5422, 8015.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 743.7 ns | 6.78 ns | 6.34 ns | 2.93 KB |
| Imposter | 1,384.7 ns | 10.61 ns | 9.93 ns | 10.59 KB |
| Mockolate | 707.3 ns | 14.15 ns | 14.53 ns | 3.07 KB |
| Moq | 111,746.9 ns | 736.94 ns | 653.28 ns | 16.53 KB |
| NSubstitute | 11,563.4 ns | 58.98 ns | 52.29 ns | 20.31 KB |
| FakeItEasy | 7,942.7 ns | 122.18 ns | 114.29 ns | 11.82 KB |

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
  y-axis "Time (ns)" 0 --> 134097
  bar [743.7, 1384.7, 707.3, 111746.9, 11563.4, 7942.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-16T03:23:00.282Z*
