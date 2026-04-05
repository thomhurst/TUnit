---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 651.9 ns | 2.29 ns | 1.91 ns | 3.06 KB |
| Imposter | 483.2 ns | 3.31 ns | 3.09 ns | 2.66 KB |
| Mockolate | 540.0 ns | 10.61 ns | 18.59 ns | 1.8 KB |
| Moq | 184,643.6 ns | 1,840.32 ns | 1,721.44 ns | 13.14 KB |
| NSubstitute | 4,630.0 ns | 48.81 ns | 45.66 ns | 7.93 KB |
| FakeItEasy | 5,166.1 ns | 84.52 ns | 79.06 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 221573
  bar [651.9, 483.2, 540, 184643.6, 4630, 5166.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 772.7 ns | 4.57 ns | 4.27 ns | 3.23 KB |
| Imposter | 541.6 ns | 10.67 ns | 15.97 ns | 2.82 KB |
| Mockolate | 662.3 ns | 11.28 ns | 10.55 ns | 2.13 KB |
| Moq | 194,142.1 ns | 1,422.32 ns | 1,330.44 ns | 13.84 KB |
| NSubstitute | 5,128.9 ns | 70.64 ns | 62.62 ns | 8.53 KB |
| FakeItEasy | 6,309.5 ns | 114.70 ns | 101.68 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 232971
  bar [772.7, 541.6, 662.3, 194142.1, 5128.9, 6309.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-05T03:32:35.400Z*
