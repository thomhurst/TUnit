---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 525.7 ns | 10.23 ns | 12.17 ns | 2.38 KB |
| Imposter | 302.1 ns | 5.71 ns | 6.58 ns | 1.33 KB |
| Mockolate | 298.2 ns | 5.83 ns | 7.37 ns | 1.39 KB |
| Moq | 478,985.3 ns | 2,307.23 ns | 2,045.30 ns | 28.52 KB |
| NSubstitute | 6,345.5 ns | 86.30 ns | 80.72 ns | 8.97 KB |
| FakeItEasy | 7,538.4 ns | 139.72 ns | 130.70 ns | 10.14 KB |

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
  y-axis "Time (ns)" 0 --> 574783
  bar [525.7, 302.1, 298.2, 478985.3, 6345.5, 7538.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 743.6 ns | 14.67 ns | 27.55 ns | 3.2 KB |
| Imposter | 550.2 ns | 10.90 ns | 18.81 ns | 2.48 KB |
| Mockolate | 513.1 ns | 9.94 ns | 10.63 ns | 2.31 KB |
| Moq | 113,429.3 ns | 525.08 ns | 491.16 ns | 15.89 KB |
| NSubstitute | 12,406.8 ns | 108.79 ns | 96.44 ns | 20.5 KB |
| FakeItEasy | 7,530.9 ns | 100.50 ns | 94.01 ns | 11.36 KB |

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
  y-axis "Time (ns)" 0 --> 136116
  bar [743.6, 550.2, 513.1, 113429.3, 12406.8, 7530.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-18T02:25:26.946Z*
