---
title: "Mock Benchmark: Callback"
description: "Callback registration and execution — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 2
---

# Callback Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Callback registration and execution:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 675.7 ns | 13.02 ns | 11.54 ns | 3.13 KB |
| Imposter | 457.7 ns | 8.52 ns | 11.37 ns | 2.66 KB |
| Mockolate | 510.7 ns | 8.04 ns | 9.88 ns | 1.8 KB |
| Moq | 184,089.7 ns | 1,673.41 ns | 1,483.43 ns | 13.14 KB |
| NSubstitute | 4,677.0 ns | 59.31 ns | 52.58 ns | 7.93 KB |
| FakeItEasy | 5,482.9 ns | 48.31 ns | 42.83 ns | 7.44 KB |

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
  y-axis "Time (ns)" 0 --> 220908
  bar [675.7, 457.7, 510.7, 184089.7, 4677, 5482.9]
```

---

### with args

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 846.7 ns | 16.85 ns | 31.23 ns | 3.22 KB |
| Imposter | 568.3 ns | 11.20 ns | 18.72 ns | 2.82 KB |
| Mockolate | 682.2 ns | 13.67 ns | 19.60 ns | 2.13 KB |
| Moq | 190,202.5 ns | 995.51 ns | 931.20 ns | 13.73 KB |
| NSubstitute | 5,070.5 ns | 99.72 ns | 93.28 ns | 8.53 KB |
| FakeItEasy | 6,256.1 ns | 90.28 ns | 80.03 ns | 9.26 KB |

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
  y-axis "Time (ns)" 0 --> 228243
  bar [846.7, 568.3, 682.2, 190202.5, 5070.5, 6256.1]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-14T03:22:19.526Z*
