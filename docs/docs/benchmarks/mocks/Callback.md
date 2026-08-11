---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 755.0 ns | 14.33 ns | 14.72 ns | 3.11 KB |
| Imposter | 513.0 ns | 9.75 ns | 11.61 ns | 2.66 KB |
| Mockolate | 405.4 ns | 7.48 ns | 6.63 ns | 1.8 KB |
| Moq | 139,780.7 ns | 1,233.48 ns | 1,030.02 ns | 13.29 KB |
| NSubstitute | 4,317.4 ns | 29.88 ns | 26.49 ns | 7.85 KB |
| FakeItEasy | 5,100.1 ns | 36.78 ns | 34.40 ns | 7.43 KB |

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
  y-axis "Time (ns)" 0 --> 167737
  bar [755, 513, 405.4, 139780.7, 4317.4, 5100.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 945.9 ns | 6.15 ns | 4.80 ns | 3.2 KB |
| Imposter | 567.6 ns | 10.98 ns | 11.75 ns | 2.82 KB |
| Mockolate | 455.8 ns | 6.10 ns | 5.71 ns | 1.84 KB |
| Moq | 145,013.0 ns | 1,160.82 ns | 1,029.03 ns | 13.75 KB |
| NSubstitute | 4,942.4 ns | 43.79 ns | 40.96 ns | 8.41 KB |
| FakeItEasy | 6,264.6 ns | 58.81 ns | 52.14 ns | 9.38 KB |

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
  y-axis "Time (ns)" 0 --> 174016
  bar [945.9, 567.6, 455.8, 145013, 4942.4, 6264.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-11T02:59:33.302Z*
