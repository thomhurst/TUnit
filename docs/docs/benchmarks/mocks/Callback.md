---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 739.5 ns | 14.03 ns | 13.12 ns | 3.08 KB |
| Imposter | 506.3 ns | 4.31 ns | 3.82 ns | 2.66 KB |
| Mockolate | 356.2 ns | 6.53 ns | 6.11 ns | 1.91 KB |
| Moq | 140,068.2 ns | 1,707.48 ns | 1,597.18 ns | 13.14 KB |
| NSubstitute | 4,731.9 ns | 48.56 ns | 43.05 ns | 7.93 KB |
| FakeItEasy | 4,958.9 ns | 68.97 ns | 64.52 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 168082
  bar [739.5, 506.3, 356.2, 140068.2, 4731.9, 4958.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 839.2 ns | 16.71 ns | 26.01 ns | 3.16 KB |
| Imposter | 597.4 ns | 3.27 ns | 2.90 ns | 2.82 KB |
| Mockolate | 402.7 ns | 4.73 ns | 4.19 ns | 1.95 KB |
| Moq | 148,955.1 ns | 891.28 ns | 744.26 ns | 13.73 KB |
| NSubstitute | 5,010.7 ns | 20.77 ns | 19.43 ns | 8.53 KB |
| FakeItEasy | 6,119.5 ns | 31.14 ns | 26.01 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 178747
  bar [839.2, 597.4, 402.7, 148955.1, 5010.7, 6119.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-21T03:28:27.059Z*
