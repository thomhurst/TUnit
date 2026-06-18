---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 643.9 ns | 12.71 ns | 12.49 ns | 2.34 KB |
| Imposter | 1,010.5 ns | 13.67 ns | 12.12 ns | 6.12 KB |
| Mockolate | 404.9 ns | 4.96 ns | 4.64 ns | 1.65 KB |
| Moq | 320,796.8 ns | 2,492.44 ns | 2,081.30 ns | 28.67 KB |
| NSubstitute | 5,663.0 ns | 19.62 ns | 17.40 ns | 9.01 KB |
| FakeItEasy | 7,755.5 ns | 25.68 ns | 22.76 ns | 10.46 KB |

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
  y-axis "Time (ns)" 0 --> 384957
  bar [643.9, 1010.5, 404.9, 320796.8, 5663, 7755.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 954.4 ns | 10.46 ns | 9.27 ns | 3.15 KB |
| Imposter | 1,705.0 ns | 33.68 ns | 71.78 ns | 10.59 KB |
| Mockolate | 675.3 ns | 7.09 ns | 6.63 ns | 2.6 KB |
| Moq | 85,539.5 ns | 342.14 ns | 285.70 ns | 16.53 KB |
| NSubstitute | 12,031.9 ns | 52.50 ns | 49.11 ns | 20.5 KB |
| FakeItEasy | 7,525.0 ns | 60.02 ns | 53.21 ns | 11.72 KB |

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
  y-axis "Time (ns)" 0 --> 102648
  bar [954.4, 1705, 675.3, 85539.5, 12031.9, 7525]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-18T03:29:53.480Z*
