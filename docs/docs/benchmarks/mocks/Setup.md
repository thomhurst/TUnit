---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 576.5 ns | 9.98 ns | 9.33 ns | 2.34 KB |
| Imposter | 695.1 ns | 7.88 ns | 7.73 ns | 6.12 KB |
| Mockolate | 324.3 ns | 4.96 ns | 6.44 ns | 1.41 KB |
| Moq | 191,348.4 ns | 2,163.96 ns | 1,918.30 ns | 28.46 KB |
| NSubstitute | 5,669.8 ns | 99.78 ns | 88.45 ns | 9.06 KB |
| FakeItEasy | 5,613.9 ns | 57.22 ns | 53.52 ns | 10.44 KB |

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
  y-axis "Time (ns)" 0 --> 229619
  bar [576.5, 695.1, 324.3, 191348.4, 5669.8, 5613.9]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 806.6 ns | 15.87 ns | 25.16 ns | 3.15 KB |
| Imposter | 1,218.2 ns | 23.62 ns | 33.11 ns | 10.59 KB |
| Mockolate | 543.2 ns | 10.76 ns | 18.27 ns | 2.35 KB |
| Moq | 52,879.2 ns | 808.72 ns | 756.47 ns | 16.63 KB |
| NSubstitute | 9,839.9 ns | 120.18 ns | 112.42 ns | 20.31 KB |
| FakeItEasy | 5,638.6 ns | 109.30 ns | 138.22 ns | 11.81 KB |

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
  y-axis "Time (ns)" 0 --> 63456
  bar [806.6, 1218.2, 543.2, 52879.2, 9839.9, 5638.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-24T02:33:35.117Z*
