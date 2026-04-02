---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 720.1 ns | 8.63 ns | 7.65 ns | 3.16 KB |
| Imposter | 502.2 ns | 8.42 ns | 7.87 ns | 2.66 KB |
| Mockolate | 548.4 ns | 9.12 ns | 8.53 ns | 1.78 KB |
| Moq | 184,237.4 ns | 699.61 ns | 620.19 ns | 13.14 KB |
| NSubstitute | 4,398.4 ns | 28.33 ns | 25.12 ns | 7.93 KB |
| FakeItEasy | 5,376.3 ns | 79.92 ns | 74.76 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221085
  bar [720.1, 502.2, 548.4, 184237.4, 4398.4, 5376.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 816.9 ns | 9.34 ns | 8.28 ns | 3.33 KB |
| Imposter | 541.3 ns | 8.12 ns | 7.20 ns | 2.82 KB |
| Mockolate | 669.9 ns | 10.33 ns | 9.66 ns | 2.11 KB |
| Moq | 190,578.4 ns | 1,266.00 ns | 1,184.22 ns | 13.73 KB |
| NSubstitute | 5,495.1 ns | 45.09 ns | 42.17 ns | 8.53 KB |
| FakeItEasy | 6,508.3 ns | 100.87 ns | 84.23 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 228695
  bar [816.9, 541.3, 669.9, 190578.4, 5495.1, 6508.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-02T03:22:36.142Z*
