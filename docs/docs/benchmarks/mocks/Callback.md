---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 646.2 ns | 1.35 ns | 1.27 ns | 3.11 KB |
| Imposter | 458.2 ns | 0.93 ns | 0.82 ns | 2.66 KB |
| Mockolate | 346.3 ns | 1.98 ns | 1.85 ns | 1.8 KB |
| Moq | 136,129.3 ns | 1,581.40 ns | 1,479.24 ns | 13.24 KB |
| NSubstitute | 4,399.0 ns | 18.17 ns | 14.19 ns | 7.85 KB |
| FakeItEasy | 4,673.8 ns | 17.45 ns | 15.47 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163356
  bar [646.2, 458.2, 346.3, 136129.3, 4399, 4673.8]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 758.4 ns | 2.97 ns | 2.78 ns | 3.2 KB |
| Imposter | 535.0 ns | 1.35 ns | 1.13 ns | 2.82 KB |
| Mockolate | 392.5 ns | 2.16 ns | 1.80 ns | 1.84 KB |
| Moq | 143,740.6 ns | 916.80 ns | 812.72 ns | 13.73 KB |
| NSubstitute | 5,042.0 ns | 14.70 ns | 12.28 ns | 8.41 KB |
| FakeItEasy | 5,593.6 ns | 36.77 ns | 34.40 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 172489
  bar [758.4, 535, 392.5, 143740.6, 5042, 5593.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-20T02:41:11.657Z*
