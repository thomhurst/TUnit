---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 651.5 ns | 12.86 ns | 16.26 ns | 3.08 KB |
| Imposter | 462.3 ns | 2.34 ns | 2.19 ns | 2.66 KB |
| Mockolate | 343.5 ns | 1.08 ns | 1.01 ns | 1.91 KB |
| Moq | 134,795.9 ns | 610.17 ns | 509.52 ns | 13.29 KB |
| NSubstitute | 4,047.0 ns | 9.55 ns | 8.93 ns | 7.93 KB |
| FakeItEasy | 4,525.1 ns | 21.74 ns | 20.33 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 161756
  bar [651.5, 462.3, 343.5, 134795.9, 4047, 4525.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 741.8 ns | 2.25 ns | 2.10 ns | 3.16 KB |
| Imposter | 534.9 ns | 1.78 ns | 1.58 ns | 2.82 KB |
| Mockolate | 388.1 ns | 0.78 ns | 0.69 ns | 1.95 KB |
| Moq | 142,841.2 ns | 729.74 ns | 609.37 ns | 13.73 KB |
| NSubstitute | 4,551.8 ns | 19.29 ns | 18.04 ns | 8.53 KB |
| FakeItEasy | 5,486.4 ns | 52.11 ns | 46.20 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 171410
  bar [741.8, 534.9, 388.1, 142841.2, 4551.8, 5486.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-23T03:25:20.859Z*
