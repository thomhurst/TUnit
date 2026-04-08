---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 537.9 ns | 7.59 ns | 6.73 ns | 2.34 KB |
| Imposter | 807.6 ns | 15.95 ns | 30.73 ns | 6.12 KB |
| Mockolate | 443.5 ns | 8.64 ns | 8.48 ns | 2.03 KB |
| Moq | 426,929.6 ns | 2,319.73 ns | 2,056.38 ns | 28.52 KB |
| NSubstitute | 5,439.6 ns | 57.07 ns | 53.38 ns | 9.01 KB |
| FakeItEasy | 7,932.6 ns | 95.53 ns | 79.77 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 512316
  bar [537.9, 807.6, 443.5, 426929.6, 5439.6, 7932.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 709.1 ns | 13.80 ns | 21.48 ns | 2.93 KB |
| Imposter | 1,457.6 ns | 28.42 ns | 30.41 ns | 10.59 KB |
| Mockolate | 750.5 ns | 11.63 ns | 10.88 ns | 3.07 KB |
| Moq | 117,302.6 ns | 749.52 ns | 701.10 ns | 16.53 KB |
| NSubstitute | 12,338.8 ns | 207.97 ns | 194.53 ns | 20.31 KB |
| FakeItEasy | 8,335.2 ns | 116.64 ns | 103.40 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 140764
  bar [709.1, 1457.6, 750.5, 117302.6, 12338.8, 8335.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-08T03:21:46.624Z*
