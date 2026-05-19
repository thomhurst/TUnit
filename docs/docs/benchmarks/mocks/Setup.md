---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 441.1 ns | 8.70 ns | 11.91 ns | 2.01 KB |
| Imposter | 857.3 ns | 17.15 ns | 48.38 ns | 6.12 KB |
| Mockolate | 363.9 ns | 7.23 ns | 7.10 ns | 1.65 KB |
| Moq | 424,831.5 ns | 3,399.51 ns | 3,179.90 ns | 28.52 KB |
| NSubstitute | 5,429.2 ns | 38.89 ns | 32.47 ns | 9.01 KB |
| FakeItEasy | 7,914.2 ns | 27.64 ns | 24.50 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 509798
  bar [441.1, 857.3, 363.9, 424831.5, 5429.2, 7914.2]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 590.3 ns | 4.95 ns | 4.39 ns | 2.59 KB |
| Imposter | 1,336.5 ns | 20.21 ns | 18.91 ns | 10.59 KB |
| Mockolate | 570.7 ns | 7.91 ns | 7.40 ns | 2.6 KB |
| Moq | 115,018.6 ns | 798.35 ns | 746.78 ns | 16.53 KB |
| NSubstitute | 12,279.8 ns | 155.38 ns | 145.35 ns | 20.5 KB |
| FakeItEasy | 8,047.2 ns | 154.20 ns | 151.44 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 138023
  bar [590.3, 1336.5, 570.7, 115018.6, 12279.8, 8047.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-19T03:26:57.825Z*
