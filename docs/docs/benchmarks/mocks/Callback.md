---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 694.7 ns | 13.74 ns | 20.98 ns | 3.16 KB |
| Imposter | 518.4 ns | 5.34 ns | 5.00 ns | 2.66 KB |
| Mockolate | 570.1 ns | 6.83 ns | 6.05 ns | 1.78 KB |
| Moq | 184,956.2 ns | 1,168.78 ns | 1,093.28 ns | 13.14 KB |
| NSubstitute | 4,652.2 ns | 38.92 ns | 36.41 ns | 7.93 KB |
| FakeItEasy | 5,477.0 ns | 73.22 ns | 68.49 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221948
  bar [694.7, 518.4, 570.1, 184956.2, 4652.2, 5477]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 854.9 ns | 16.89 ns | 19.45 ns | 3.33 KB |
| Imposter | 553.0 ns | 10.93 ns | 22.82 ns | 2.82 KB |
| Mockolate | 666.6 ns | 11.54 ns | 10.80 ns | 2.11 KB |
| Moq | 190,837.9 ns | 1,346.17 ns | 1,259.21 ns | 13.84 KB |
| NSubstitute | 5,149.2 ns | 73.70 ns | 65.34 ns | 8.53 KB |
| FakeItEasy | 6,425.4 ns | 108.92 ns | 101.88 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 229006
  bar [854.9, 553, 666.6, 190837.9, 5149.2, 6425.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-31T03:22:46.140Z*
