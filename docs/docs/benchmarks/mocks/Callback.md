---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 649.1 ns | 5.73 ns | 4.79 ns | 2.98 KB |
| Imposter | 468.5 ns | 4.37 ns | 4.09 ns | 2.66 KB |
| Mockolate | 352.7 ns | 2.48 ns | 2.20 ns | 1.91 KB |
| Moq | 135,688.9 ns | 1,129.76 ns | 1,001.50 ns | 13.14 KB |
| NSubstitute | 4,089.1 ns | 29.82 ns | 26.43 ns | 7.93 KB |
| FakeItEasy | 4,391.9 ns | 45.73 ns | 40.54 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 162827
  bar [649.1, 468.5, 352.7, 135688.9, 4089.1, 4391.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 720.2 ns | 1.71 ns | 1.60 ns | 3.06 KB |
| Imposter | 544.3 ns | 1.09 ns | 0.97 ns | 2.82 KB |
| Mockolate | 387.6 ns | 4.02 ns | 3.76 ns | 1.95 KB |
| Moq | 140,692.8 ns | 889.18 ns | 742.51 ns | 13.73 KB |
| NSubstitute | 4,615.0 ns | 29.97 ns | 28.04 ns | 8.53 KB |
| FakeItEasy | 5,496.1 ns | 13.47 ns | 12.60 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 168832
  bar [720.2, 544.3, 387.6, 140692.8, 4615, 5496.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-20T03:28:07.578Z*
