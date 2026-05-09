---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 461.5 ns | 4.03 ns | 3.57 ns | 2.01 KB |
| Imposter | 802.1 ns | 4.15 ns | 3.24 ns | 6.12 KB |
| Mockolate | 396.1 ns | 6.84 ns | 6.40 ns | 1.68 KB |
| Moq | 294,184.3 ns | 881.62 ns | 736.19 ns | 28.52 KB |
| NSubstitute | 5,254.5 ns | 25.68 ns | 24.02 ns | 9.06 KB |
| FakeItEasy | 6,883.3 ns | 57.68 ns | 51.14 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 353022
  bar [461.5, 802.1, 396.1, 294184.3, 5254.5, 6883.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 637.8 ns | 3.49 ns | 3.27 ns | 2.59 KB |
| Imposter | 1,319.4 ns | 3.30 ns | 2.76 ns | 10.59 KB |
| Mockolate | 645.4 ns | 3.07 ns | 2.87 ns | 2.82 KB |
| Moq | 89,238.5 ns | 455.05 ns | 403.39 ns | 16.61 KB |
| NSubstitute | 10,605.0 ns | 52.79 ns | 46.80 ns | 20.31 KB |
| FakeItEasy | 6,621.2 ns | 44.33 ns | 41.47 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 107087
  bar [637.8, 1319.4, 645.4, 89238.5, 10605, 6621.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-09T03:26:33.451Z*
