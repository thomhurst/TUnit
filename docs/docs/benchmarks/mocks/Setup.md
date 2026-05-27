---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 499.0 ns | 4.50 ns | 3.99 ns | 2.31 KB |
| Imposter | 835.6 ns | 11.62 ns | 10.87 ns | 6.12 KB |
| Mockolate | 325.1 ns | 4.27 ns | 3.78 ns | 1.65 KB |
| Moq | 422,785.0 ns | 1,742.19 ns | 1,454.80 ns | 28.52 KB |
| NSubstitute | 5,450.0 ns | 94.05 ns | 96.58 ns | 9.06 KB |
| FakeItEasy | 7,651.3 ns | 55.78 ns | 52.18 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 507342
  bar [499, 835.6, 325.1, 422785, 5450, 7651.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 716.9 ns | 5.44 ns | 4.82 ns | 3.09 KB |
| Imposter | 1,287.4 ns | 10.14 ns | 8.99 ns | 10.59 KB |
| Mockolate | 527.8 ns | 4.25 ns | 3.77 ns | 2.6 KB |
| Moq | 112,405.2 ns | 456.29 ns | 381.02 ns | 16.53 KB |
| NSubstitute | 11,637.2 ns | 101.02 ns | 94.50 ns | 20.31 KB |
| FakeItEasy | 7,942.5 ns | 46.50 ns | 43.50 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134887
  bar [716.9, 1287.4, 527.8, 112405.2, 11637.2, 7942.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-27T03:29:35.677Z*
