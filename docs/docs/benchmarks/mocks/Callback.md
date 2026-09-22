---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 661.9 ns | 3.59 ns | 3.00 ns | 3.11 KB |
| Imposter | 491.2 ns | 6.60 ns | 5.85 ns | 2.66 KB |
| Mockolate | 350.2 ns | 5.66 ns | 5.01 ns | 1.8 KB |
| Moq | 186,564.4 ns | 1,312.80 ns | 1,227.99 ns | 13.14 KB |
| NSubstitute | 4,755.2 ns | 37.46 ns | 35.04 ns | 7.85 KB |
| FakeItEasy | 5,341.6 ns | 73.63 ns | 68.87 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223878
  bar [661.9, 491.2, 350.2, 186564.4, 4755.2, 5341.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 787.3 ns | 6.40 ns | 5.35 ns | 3.2 KB |
| Imposter | 527.0 ns | 4.20 ns | 3.92 ns | 2.82 KB |
| Mockolate | 418.2 ns | 7.48 ns | 7.35 ns | 1.84 KB |
| Moq | 194,058.9 ns | 1,237.70 ns | 1,157.75 ns | 13.73 KB |
| NSubstitute | 5,684.2 ns | 59.08 ns | 55.27 ns | 8.41 KB |
| FakeItEasy | 6,403.4 ns | 92.06 ns | 86.11 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 232871
  bar [787.3, 527, 418.2, 194058.9, 5684.2, 6403.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-22T02:33:33.738Z*
