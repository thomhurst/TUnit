---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 652.9 ns | 1.97 ns | 1.65 ns | 3.11 KB |
| Imposter | 464.3 ns | 1.13 ns | 1.06 ns | 2.66 KB |
| Mockolate | 347.2 ns | 2.05 ns | 1.91 ns | 1.8 KB |
| Moq | 136,337.3 ns | 867.15 ns | 768.70 ns | 13.29 KB |
| NSubstitute | 4,526.5 ns | 48.31 ns | 40.34 ns | 7.85 KB |
| FakeItEasy | 4,579.6 ns | 34.68 ns | 32.44 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 163605
  bar [652.9, 464.3, 347.2, 136337.3, 4526.5, 4579.6]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 769.9 ns | 3.36 ns | 2.98 ns | 3.2 KB |
| Imposter | 539.0 ns | 0.89 ns | 0.84 ns | 2.82 KB |
| Mockolate | 408.0 ns | 1.23 ns | 1.15 ns | 1.84 KB |
| Moq | 144,564.3 ns | 1,112.84 ns | 929.27 ns | 13.73 KB |
| NSubstitute | 5,011.5 ns | 21.85 ns | 18.24 ns | 8.41 KB |
| FakeItEasy | 5,548.6 ns | 31.25 ns | 26.09 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 173478
  bar [769.9, 539, 408, 144564.3, 5011.5, 5548.6]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-18T02:39:29.373Z*
