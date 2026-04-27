---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 463.2 ns | 3.35 ns | 3.13 ns | 2.01 KB |
| Imposter | 837.8 ns | 10.06 ns | 9.41 ns | 6.12 KB |
| Mockolate | 464.4 ns | 9.36 ns | 12.17 ns | 2.03 KB |
| Moq | 430,142.9 ns | 1,611.37 ns | 1,507.28 ns | 28.66 KB |
| NSubstitute | 5,617.9 ns | 23.05 ns | 19.25 ns | 9.01 KB |
| FakeItEasy | 7,994.6 ns | 97.79 ns | 86.68 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 516172
  bar [463.2, 837.8, 464.4, 430142.9, 5617.9, 7994.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 649.6 ns | 11.28 ns | 14.66 ns | 2.59 KB |
| Imposter | 1,519.3 ns | 28.00 ns | 42.76 ns | 10.59 KB |
| Mockolate | 747.1 ns | 12.42 ns | 11.01 ns | 3.07 KB |
| Moq | 117,070.0 ns | 1,175.03 ns | 1,041.64 ns | 16.61 KB |
| NSubstitute | 12,401.2 ns | 169.65 ns | 158.69 ns | 20.31 KB |
| FakeItEasy | 7,976.4 ns | 128.45 ns | 113.87 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 140484
  bar [649.6, 1519.3, 747.1, 117070, 12401.2, 7976.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-27T03:25:25.011Z*
