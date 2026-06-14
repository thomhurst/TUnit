---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 483.2 ns | 6.00 ns | 5.61 ns | 2.34 KB |
| Imposter | 741.2 ns | 8.40 ns | 7.86 ns | 6.12 KB |
| Mockolate | 314.3 ns | 4.28 ns | 4.01 ns | 1.65 KB |
| Moq | 414,002.2 ns | 6,040.56 ns | 5,650.34 ns | 28.74 KB |
| NSubstitute | 5,239.1 ns | 56.36 ns | 49.96 ns | 9.06 KB |
| FakeItEasy | 8,339.0 ns | 110.81 ns | 103.65 ns | 10.56 KB |

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
  y-axis "Time (ns)" 0 --> 496803
  bar [483.2, 741.2, 314.3, 414002.2, 5239.1, 8339]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 704.9 ns | 8.65 ns | 8.09 ns | 3.15 KB |
| Imposter | 1,270.2 ns | 18.19 ns | 16.12 ns | 10.59 KB |
| Mockolate | 537.9 ns | 8.39 ns | 7.85 ns | 2.6 KB |
| Moq | 111,180.3 ns | 2,211.64 ns | 2,366.43 ns | 16.53 KB |
| NSubstitute | 11,958.1 ns | 235.47 ns | 280.32 ns | 20.47 KB |
| FakeItEasy | 7,848.8 ns | 105.95 ns | 99.11 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 133417
  bar [704.9, 1270.2, 537.9, 111180.3, 11958.1, 7848.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-14T03:35:08.044Z*
