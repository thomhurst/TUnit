---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 5
---

# MockCreation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock instance creation performance:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 43.05 ns | 0.926 ns | 0.951 ns | 208 B |
| Imposter | 105.64 ns | 2.192 ns | 2.524 ns | 440 B |
| Mockolate | 75.63 ns | 1.571 ns | 3.102 ns | 376 B |
| Moq | 1,243.46 ns | 24.208 ns | 26.907 ns | 2048 B |
| NSubstitute | 1,903.16 ns | 33.245 ns | 31.097 ns | 5000 B |
| FakeItEasy | 1,721.69 ns | 31.543 ns | 27.962 ns | 2723 B |

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
  y-axis "Time (ns)" 0 --> 2284
  bar [43.05, 105.64, 75.63, 1243.46, 1903.16, 1721.69]
```

---

### Repository

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 38.20 ns | 0.832 ns | 1.623 ns | 208 B |
| Imposter | 165.61 ns | 3.286 ns | 3.652 ns | 696 B |
| Mockolate | 79.61 ns | 1.525 ns | 1.426 ns | 376 B |
| Moq | 1,253.29 ns | 5.107 ns | 4.264 ns | 1912 B |
| NSubstitute | 1,766.91 ns | 14.860 ns | 13.173 ns | 5000 B |
| FakeItEasy | 1,697.93 ns | 17.302 ns | 15.338 ns | 2723 B |

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
  y-axis "Time (ns)" 0 --> 2121
  bar [38.2, 165.61, 79.61, 1253.29, 1766.91, 1697.93]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-04T03:18:30.135Z*
