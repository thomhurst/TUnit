---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 426.9 ns | 4.26 ns | 3.98 ns | 2.01 KB |
| Imposter | 811.2 ns | 11.63 ns | 10.31 ns | 6.12 KB |
| Mockolate | 449.8 ns | 3.55 ns | 3.32 ns | 2.03 KB |
| Moq | 421,300.1 ns | 1,715.65 ns | 1,432.65 ns | 28.52 KB |
| NSubstitute | 5,497.9 ns | 67.63 ns | 59.95 ns | 9.01 KB |
| FakeItEasy | 8,140.4 ns | 108.90 ns | 96.54 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 505561
  bar [426.9, 811.2, 449.8, 421300.1, 5497.9, 8140.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 619.0 ns | 4.45 ns | 3.94 ns | 2.59 KB |
| Imposter | 1,370.3 ns | 8.05 ns | 7.53 ns | 10.59 KB |
| Mockolate | 691.1 ns | 4.98 ns | 4.42 ns | 3.07 KB |
| Moq | 114,753.7 ns | 406.61 ns | 339.54 ns | 16.53 KB |
| NSubstitute | 12,279.2 ns | 67.94 ns | 60.23 ns | 20.5 KB |
| FakeItEasy | 7,934.9 ns | 131.66 ns | 123.15 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 137705
  bar [619, 1370.3, 691.1, 114753.7, 12279.2, 7934.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-25T03:21:02.718Z*
