---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 832.4 ns | 8.02 ns | 7.50 ns | 3.08 KB |
| Imposter | 587.1 ns | 8.39 ns | 7.85 ns | 2.66 KB |
| Mockolate | 455.5 ns | 3.41 ns | 2.85 ns | 1.91 KB |
| Moq | 141,994.4 ns | 1,233.98 ns | 1,154.26 ns | 13.15 KB |
| NSubstitute | 4,313.2 ns | 9.76 ns | 8.65 ns | 7.93 KB |
| FakeItEasy | 5,209.3 ns | 17.46 ns | 15.48 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 170394
  bar [832.4, 587.1, 455.5, 141994.4, 4313.2, 5209.3]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 962.5 ns | 5.07 ns | 4.74 ns | 3.16 KB |
| Imposter | 658.8 ns | 5.48 ns | 5.13 ns | 2.82 KB |
| Mockolate | 508.6 ns | 3.13 ns | 2.93 ns | 1.95 KB |
| Moq | 148,935.9 ns | 891.39 ns | 790.19 ns | 13.75 KB |
| NSubstitute | 4,866.4 ns | 10.63 ns | 9.94 ns | 8.53 KB |
| FakeItEasy | 6,709.2 ns | 26.58 ns | 23.57 ns | 9.41 KB |

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
  y-axis "Time (ns)" 0 --> 178724
  bar [962.5, 658.8, 508.6, 148935.9, 4866.4, 6709.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-30T03:25:40.021Z*
