---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 531.7 ns | 7.92 ns | 11.10 ns | 1.99 KB |
| Imposter | 831.7 ns | 16.57 ns | 30.71 ns | 6.12 KB |
| Mockolate | 464.2 ns | 9.27 ns | 9.92 ns | 2.02 KB |
| Moq | 429,953.6 ns | 2,253.38 ns | 1,881.67 ns | 28.52 KB |
| NSubstitute | 5,521.3 ns | 80.20 ns | 75.02 ns | 9.01 KB |
| FakeItEasy | 8,160.3 ns | 41.63 ns | 36.90 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 515945
  bar [531.7, 831.7, 464.2, 429953.6, 5521.3, 8160.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 803.0 ns | 13.78 ns | 12.89 ns | 2.75 KB |
| Imposter | 1,409.6 ns | 27.42 ns | 30.48 ns | 10.59 KB |
| Mockolate | 741.1 ns | 14.11 ns | 14.49 ns | 3.06 KB |
| Moq | 115,088.7 ns | 549.39 ns | 458.77 ns | 16.64 KB |
| NSubstitute | 12,310.4 ns | 138.56 ns | 122.83 ns | 20.47 KB |
| FakeItEasy | 7,799.4 ns | 128.28 ns | 120.00 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 138107
  bar [803, 1409.6, 741.1, 115088.7, 12310.4, 7799.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-04T03:18:30.135Z*
