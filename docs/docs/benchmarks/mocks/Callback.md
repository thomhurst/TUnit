---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 719.4 ns | 4.14 ns | 3.87 ns | 3.06 KB |
| Imposter | 448.2 ns | 1.97 ns | 1.65 ns | 2.66 KB |
| Mockolate | 491.8 ns | 3.61 ns | 3.20 ns | 1.8 KB |
| Moq | 182,551.5 ns | 686.03 ns | 641.71 ns | 13.14 KB |
| NSubstitute | 4,534.1 ns | 19.45 ns | 17.24 ns | 7.93 KB |
| FakeItEasy | 5,100.9 ns | 19.36 ns | 18.11 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 219062
  bar [719.4, 448.2, 491.8, 182551.5, 4534.1, 5100.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 786.2 ns | 13.10 ns | 12.25 ns | 3.15 KB |
| Imposter | 529.7 ns | 7.80 ns | 7.29 ns | 2.82 KB |
| Mockolate | 651.3 ns | 12.45 ns | 13.83 ns | 2.13 KB |
| Moq | 191,221.7 ns | 1,816.65 ns | 1,610.42 ns | 13.73 KB |
| NSubstitute | 5,182.6 ns | 34.10 ns | 30.23 ns | 8.53 KB |
| FakeItEasy | 5,968.9 ns | 88.19 ns | 78.18 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 229467
  bar [786.2, 529.7, 651.3, 191221.7, 5182.6, 5968.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T03:22:20.916Z*
