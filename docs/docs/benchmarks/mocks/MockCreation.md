---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 5
---

# MockCreation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock instance creation performance:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 35.02 ns | 0.764 ns | 0.909 ns | 200 B |
| Imposter | 90.61 ns | 1.724 ns | 2.581 ns | 440 B |
| Mockolate | 63.79 ns | 1.341 ns | 1.317 ns | 384 B |
| Moq | 1,410.77 ns | 27.844 ns | 36.205 ns | 2048 B |
| NSubstitute | 2,046.34 ns | 39.539 ns | 43.948 ns | 5000 B |
| FakeItEasy | 1,736.46 ns | 28.716 ns | 33.070 ns | 2715 B |

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
  title "MockCreation Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2456
  bar [35.02, 90.61, 63.79, 1410.77, 2046.34, 1736.46]
```

---

### Repository

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 34.87 ns | 0.680 ns | 0.636 ns | 200 B |
| Imposter | 136.11 ns | 1.590 ns | 1.487 ns | 696 B |
| Mockolate | 63.06 ns | 0.550 ns | 0.488 ns | 384 B |
| Moq | 1,341.56 ns | 14.913 ns | 13.220 ns | 1912 B |
| NSubstitute | 1,931.34 ns | 21.432 ns | 20.047 ns | 5000 B |
| FakeItEasy | 1,754.72 ns | 32.927 ns | 30.800 ns | 2715 B |

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
  title "MockCreation (Repository) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 2318
  bar [34.87, 136.11, 63.06, 1341.56, 1931.34, 1754.72]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T03:22:20.916Z*
