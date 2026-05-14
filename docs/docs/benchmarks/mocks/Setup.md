---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 412.1 ns | 4.33 ns | 3.84 ns | 2.01 KB |
| Imposter | 796.8 ns | 2.65 ns | 2.35 ns | 6.12 KB |
| Mockolate | 327.1 ns | 0.92 ns | 0.81 ns | 1.65 KB |
| Moq | 423,482.9 ns | 2,174.87 ns | 2,034.37 ns | 28.52 KB |
| NSubstitute | 5,383.9 ns | 21.29 ns | 19.92 ns | 9.01 KB |
| FakeItEasy | 8,160.3 ns | 21.79 ns | 20.38 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 508180
  bar [412.1, 796.8, 327.1, 423482.9, 5383.9, 8160.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 582.1 ns | 2.47 ns | 2.31 ns | 2.59 KB |
| Imposter | 1,316.3 ns | 2.83 ns | 2.36 ns | 10.59 KB |
| Mockolate | 534.1 ns | 2.31 ns | 2.16 ns | 2.6 KB |
| Moq | 113,341.0 ns | 527.96 ns | 468.03 ns | 16.53 KB |
| NSubstitute | 11,891.6 ns | 63.01 ns | 58.94 ns | 20.66 KB |
| FakeItEasy | 7,887.0 ns | 53.73 ns | 50.26 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 136010
  bar [582.1, 1316.3, 534.1, 113341, 11891.6, 7887]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-14T03:27:14.658Z*
