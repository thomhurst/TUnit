---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 673.6 ns | 2.15 ns | 1.80 ns | 3.16 KB |
| Imposter | 439.6 ns | 3.53 ns | 3.30 ns | 2.66 KB |
| Mockolate | 496.5 ns | 1.93 ns | 1.71 ns | 1.78 KB |
| Moq | 179,400.2 ns | 1,011.49 ns | 896.66 ns | 13.14 KB |
| NSubstitute | 4,199.2 ns | 14.83 ns | 12.38 ns | 7.93 KB |
| FakeItEasy | 5,142.0 ns | 23.23 ns | 19.40 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 215281
  bar [673.6, 439.6, 496.5, 179400.2, 4199.2, 5142]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 748.8 ns | 3.00 ns | 2.80 ns | 3.33 KB |
| Imposter | 530.2 ns | 5.16 ns | 4.31 ns | 2.82 KB |
| Mockolate | 629.1 ns | 3.63 ns | 3.40 ns | 2.11 KB |
| Moq | 187,468.9 ns | 1,102.64 ns | 1,031.41 ns | 13.73 KB |
| NSubstitute | 4,883.9 ns | 19.56 ns | 16.33 ns | 8.53 KB |
| FakeItEasy | 6,105.9 ns | 117.66 ns | 120.82 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 224963
  bar [748.8, 530.2, 629.1, 187468.9, 4883.9, 6105.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-03T03:23:45.860Z*
