---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 680.3 ns | 11.50 ns | 10.76 ns | 2.98 KB |
| Imposter | 491.2 ns | 9.56 ns | 12.76 ns | 2.66 KB |
| Mockolate | 376.0 ns | 7.47 ns | 12.06 ns | 1.91 KB |
| Moq | 186,063.0 ns | 1,873.00 ns | 1,752.01 ns | 13.14 KB |
| NSubstitute | 4,514.6 ns | 47.92 ns | 40.02 ns | 7.93 KB |
| FakeItEasy | 5,261.9 ns | 51.41 ns | 42.93 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 223276
  bar [680.3, 491.2, 376, 186063, 4514.6, 5261.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 724.4 ns | 6.80 ns | 6.02 ns | 3.06 KB |
| Imposter | 529.3 ns | 6.47 ns | 5.05 ns | 2.82 KB |
| Mockolate | 420.5 ns | 8.33 ns | 12.21 ns | 1.95 KB |
| Moq | 195,133.0 ns | 2,025.90 ns | 1,895.02 ns | 13.73 KB |
| NSubstitute | 5,264.0 ns | 65.55 ns | 54.74 ns | 8.53 KB |
| FakeItEasy | 6,459.2 ns | 122.15 ns | 114.26 ns | 9.4 KB |

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
  y-axis "Time (ns)" 0 --> 234160
  bar [724.4, 529.3, 420.5, 195133, 5264, 6459.2]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-18T03:29:10.052Z*
