---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 566.8 ns | 9.71 ns | 9.09 ns | 2.34 KB |
| Imposter | 849.6 ns | 11.70 ns | 9.77 ns | 6.12 KB |
| Mockolate | 315.0 ns | 3.55 ns | 3.32 ns | 1.41 KB |
| Moq | 425,527.1 ns | 1,603.68 ns | 1,421.62 ns | 28.6 KB |
| NSubstitute | 6,262.5 ns | 29.63 ns | 26.27 ns | 9.06 KB |
| FakeItEasy | 8,567.4 ns | 99.84 ns | 88.50 ns | 10.6 KB |

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
  y-axis "Time (ns)" 0 --> 510633
  bar [566.8, 849.6, 315, 425527.1, 6262.5, 8567.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 791.6 ns | 3.89 ns | 3.45 ns | 3.15 KB |
| Imposter | 1,437.7 ns | 10.89 ns | 10.19 ns | 10.59 KB |
| Mockolate | 550.1 ns | 3.42 ns | 3.03 ns | 2.35 KB |
| Moq | 114,108.4 ns | 586.54 ns | 489.79 ns | 16.53 KB |
| NSubstitute | 12,522.7 ns | 103.70 ns | 86.60 ns | 20.31 KB |
| FakeItEasy | 8,066.5 ns | 61.42 ns | 57.46 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136931
  bar [791.6, 1437.7, 550.1, 114108.4, 12522.7, 8066.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-07T02:34:20.667Z*
