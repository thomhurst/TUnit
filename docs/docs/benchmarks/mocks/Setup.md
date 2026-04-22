---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 517.7 ns | 3.78 ns | 3.54 ns | 2.34 KB |
| Imposter | 815.2 ns | 12.55 ns | 11.74 ns | 6.12 KB |
| Mockolate | 455.6 ns | 4.06 ns | 3.60 ns | 2.03 KB |
| Moq | 426,650.9 ns | 2,039.30 ns | 1,907.56 ns | 28.66 KB |
| NSubstitute | 5,568.6 ns | 26.51 ns | 23.50 ns | 9.06 KB |
| FakeItEasy | 8,033.8 ns | 138.89 ns | 123.12 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 511982
  bar [517.7, 815.2, 455.6, 426650.9, 5568.6, 8033.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 703.5 ns | 7.16 ns | 6.70 ns | 2.93 KB |
| Imposter | 1,361.1 ns | 7.10 ns | 6.64 ns | 10.59 KB |
| Mockolate | 689.2 ns | 7.07 ns | 5.90 ns | 3.07 KB |
| Moq | 113,795.9 ns | 239.78 ns | 187.21 ns | 16.53 KB |
| NSubstitute | 11,689.7 ns | 80.46 ns | 71.33 ns | 20.31 KB |
| FakeItEasy | 7,961.4 ns | 100.60 ns | 94.11 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 136556
  bar [703.5, 1361.1, 689.2, 113795.9, 11689.7, 7961.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-22T03:22:46.937Z*
