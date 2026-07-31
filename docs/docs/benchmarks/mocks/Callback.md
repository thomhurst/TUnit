---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 659.0 ns | 13.10 ns | 12.25 ns | 3.11 KB |
| Imposter | 470.1 ns | 9.38 ns | 17.38 ns | 2.66 KB |
| Mockolate | 347.0 ns | 6.40 ns | 10.34 ns | 1.8 KB |
| Moq | 182,203.3 ns | 600.25 ns | 468.64 ns | 13.14 KB |
| NSubstitute | 4,565.6 ns | 26.23 ns | 23.25 ns | 7.85 KB |
| FakeItEasy | 5,061.6 ns | 26.26 ns | 24.57 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 218644
  bar [659, 470.1, 347, 182203.3, 4565.6, 5061.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 775.8 ns | 5.33 ns | 4.73 ns | 3.2 KB |
| Imposter | 535.8 ns | 3.78 ns | 3.54 ns | 2.82 KB |
| Mockolate | 404.2 ns | 2.65 ns | 2.35 ns | 1.84 KB |
| Moq | 189,314.0 ns | 1,281.70 ns | 1,136.20 ns | 13.73 KB |
| NSubstitute | 5,169.3 ns | 52.49 ns | 46.53 ns | 8.41 KB |
| FakeItEasy | 6,487.5 ns | 55.68 ns | 52.09 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 227177
  bar [775.8, 535.8, 404.2, 189314, 5169.3, 6487.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-31T03:21:39.823Z*
