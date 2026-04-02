---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 595.0 ns | 4.86 ns | 4.55 ns | 1.99 KB |
| Imposter | 939.3 ns | 13.84 ns | 12.95 ns | 6.12 KB |
| Mockolate | 487.5 ns | 3.88 ns | 3.44 ns | 2.01 KB |
| Moq | 429,045.4 ns | 2,154.95 ns | 1,910.30 ns | 28.52 KB |
| NSubstitute | 5,806.8 ns | 48.96 ns | 43.40 ns | 9.01 KB |
| FakeItEasy | 8,364.3 ns | 52.35 ns | 46.40 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 514855
  bar [595, 939.3, 487.5, 429045.4, 5806.8, 8364.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 853.0 ns | 8.98 ns | 8.40 ns | 2.75 KB |
| Imposter | 1,472.1 ns | 34.30 ns | 101.14 ns | 10.59 KB |
| Mockolate | 673.6 ns | 6.16 ns | 5.46 ns | 3.05 KB |
| Moq | 116,235.7 ns | 754.61 ns | 705.86 ns | 16.53 KB |
| NSubstitute | 12,008.6 ns | 75.21 ns | 70.35 ns | 20.5 KB |
| FakeItEasy | 7,578.7 ns | 147.12 ns | 130.42 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 139483
  bar [853, 1472.1, 673.6, 116235.7, 12008.6, 7578.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-02T03:22:36.142Z*
