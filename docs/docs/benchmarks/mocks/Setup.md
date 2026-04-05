---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 492.4 ns | 1.09 ns | 0.85 ns | 2.03 KB |
| Imposter | 774.2 ns | 6.49 ns | 6.07 ns | 6.12 KB |
| Mockolate | 424.9 ns | 1.96 ns | 1.53 ns | 2.03 KB |
| Moq | 421,701.0 ns | 1,383.27 ns | 1,155.09 ns | 28.76 KB |
| NSubstitute | 5,305.7 ns | 48.80 ns | 40.75 ns | 9.01 KB |
| FakeItEasy | 8,075.6 ns | 43.50 ns | 38.56 ns | 10.53 KB |

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
  y-axis "Time (ns)" 0 --> 506042
  bar [492.4, 774.2, 424.9, 421701, 5305.7, 8075.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 681.9 ns | 3.15 ns | 2.63 ns | 2.7 KB |
| Imposter | 1,306.9 ns | 13.63 ns | 12.75 ns | 10.59 KB |
| Mockolate | 663.5 ns | 2.66 ns | 2.36 ns | 3.07 KB |
| Moq | 112,440.0 ns | 780.25 ns | 691.67 ns | 16.53 KB |
| NSubstitute | 11,663.3 ns | 33.90 ns | 30.05 ns | 20.47 KB |
| FakeItEasy | 7,415.3 ns | 67.45 ns | 59.80 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134928
  bar [681.9, 1306.9, 663.5, 112440, 11663.3, 7415.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T03:32:35.400Z*
