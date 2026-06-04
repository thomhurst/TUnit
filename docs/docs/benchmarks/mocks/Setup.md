---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 682.6 ns | 19.84 ns | 58.18 ns | 2.34 KB |
| Imposter | 1,263.5 ns | 52.17 ns | 153.84 ns | 6.12 KB |
| Mockolate | 478.1 ns | 14.46 ns | 41.95 ns | 1.65 KB |
| Moq | 327,428.3 ns | 3,202.53 ns | 2,995.65 ns | 28.56 KB |
| NSubstitute | 5,781.8 ns | 110.88 ns | 127.70 ns | 9.01 KB |
| FakeItEasy | 8,248.0 ns | 158.06 ns | 155.23 ns | 10.46 KB |

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
  y-axis "Time (ns)" 0 --> 392914
  bar [682.6, 1263.5, 478.1, 327428.3, 5781.8, 8248]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,109.8 ns | 22.26 ns | 50.25 ns | 3.14 KB |
| Imposter | 2,225.0 ns | 106.76 ns | 314.78 ns | 10.59 KB |
| Mockolate | 747.3 ns | 24.35 ns | 71.41 ns | 2.6 KB |
| Moq | 89,402.7 ns | 1,255.39 ns | 1,174.29 ns | 16.53 KB |
| NSubstitute | 12,407.3 ns | 175.07 ns | 155.19 ns | 20.31 KB |
| FakeItEasy | 7,620.5 ns | 101.01 ns | 89.54 ns | 11.72 KB |

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
  y-axis "Time (ns)" 0 --> 107284
  bar [1109.8, 2225, 747.3, 89402.7, 12407.3, 7620.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-04T03:31:56.363Z*
