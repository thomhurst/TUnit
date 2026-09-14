---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 691.8 ns | 9.28 ns | 8.23 ns | 3.11 KB |
| Imposter | 493.3 ns | 8.52 ns | 10.14 ns | 2.66 KB |
| Mockolate | 364.6 ns | 4.92 ns | 4.60 ns | 1.8 KB |
| Moq | 188,486.8 ns | 1,246.17 ns | 1,165.67 ns | 13.14 KB |
| NSubstitute | 5,061.2 ns | 37.55 ns | 35.12 ns | 7.85 KB |
| FakeItEasy | 5,177.9 ns | 90.39 ns | 80.13 ns | 7.44 KB |

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
  title "Callback Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 226185
  bar [691.8, 493.3, 364.6, 188486.8, 5061.2, 5177.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 793.6 ns | 7.65 ns | 7.16 ns | 3.2 KB |
| Imposter | 528.0 ns | 5.51 ns | 5.16 ns | 2.82 KB |
| Mockolate | 390.2 ns | 3.86 ns | 3.61 ns | 1.84 KB |
| Moq | 192,995.6 ns | 997.89 ns | 933.43 ns | 13.73 KB |
| NSubstitute | 5,539.7 ns | 110.72 ns | 103.57 ns | 8.41 KB |
| FakeItEasy | 6,605.4 ns | 81.33 ns | 72.10 ns | 9.4 KB |

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
  title "Callback (with args) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 231595
  bar [793.6, 528, 390.2, 192995.6, 5539.7, 6605.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-14T02:37:23.172Z*
