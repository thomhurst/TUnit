---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 535.7 ns | 3.20 ns | 2.67 ns | 2.01 KB |
| Imposter | 823.6 ns | 15.43 ns | 15.84 ns | 6.12 KB |
| Mockolate | 478.2 ns | 5.17 ns | 4.58 ns | 2.03 KB |
| Moq | 299,075.1 ns | 2,544.75 ns | 2,124.98 ns | 28.52 KB |
| NSubstitute | 5,319.8 ns | 53.62 ns | 50.16 ns | 9.01 KB |
| FakeItEasy | 7,371.0 ns | 92.06 ns | 86.11 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 358891
  bar [535.7, 823.6, 478.2, 299075.1, 5319.8, 7371]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 710.2 ns | 7.12 ns | 5.95 ns | 2.59 KB |
| Imposter | 1,393.5 ns | 22.85 ns | 21.38 ns | 10.59 KB |
| Mockolate | 760.9 ns | 5.93 ns | 5.55 ns | 3.07 KB |
| Moq | 85,902.3 ns | 649.59 ns | 542.43 ns | 16.53 KB |
| NSubstitute | 11,020.8 ns | 67.40 ns | 63.05 ns | 20.31 KB |
| FakeItEasy | 6,944.1 ns | 70.13 ns | 58.56 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 103083
  bar [710.2, 1393.5, 760.9, 85902.3, 11020.8, 6944.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-29T03:24:49.990Z*
