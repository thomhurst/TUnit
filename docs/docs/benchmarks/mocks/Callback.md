---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 677.1 ns | 11.59 ns | 10.27 ns | 2.98 KB |
| Imposter | 515.5 ns | 10.15 ns | 17.77 ns | 2.66 KB |
| Mockolate | 404.6 ns | 7.99 ns | 10.94 ns | 1.91 KB |
| Moq | 189,867.2 ns | 1,690.68 ns | 1,581.47 ns | 13.14 KB |
| NSubstitute | 4,796.2 ns | 72.73 ns | 68.03 ns | 7.93 KB |
| FakeItEasy | 5,457.1 ns | 68.21 ns | 63.80 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 227841
  bar [677.1, 515.5, 404.6, 189867.2, 4796.2, 5457.1]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 782.9 ns | 13.63 ns | 12.75 ns | 3.06 KB |
| Imposter | 606.7 ns | 11.96 ns | 15.56 ns | 2.82 KB |
| Mockolate | 439.7 ns | 5.45 ns | 4.83 ns | 1.95 KB |
| Moq | 197,906.9 ns | 816.64 ns | 723.93 ns | 13.73 KB |
| NSubstitute | 5,165.2 ns | 59.35 ns | 55.52 ns | 8.53 KB |
| FakeItEasy | 6,777.5 ns | 67.00 ns | 59.39 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 237489
  bar [782.9, 606.7, 439.7, 197906.9, 5165.2, 6777.5]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-17T03:31:33.295Z*
