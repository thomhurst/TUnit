---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 5
---

# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Mock instance creation performance:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 32.33 ns | 0.301 ns | 0.282 ns | 192 B |
| Imposter | 104.34 ns | 1.166 ns | 1.091 ns | 440 B |
| Mockolate | 71.11 ns | 1.231 ns | 1.151 ns | 424 B |
| Moq | 1,437.93 ns | 26.092 ns | 24.406 ns | 2048 B |
| NSubstitute | 2,035.93 ns | 18.756 ns | 17.545 ns | 5000 B |
| FakeItEasy | 1,912.75 ns | 29.969 ns | 28.033 ns | 2715 B |

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
  y-axis "Time (ns)" 0 --> 2444
  bar [32.33, 104.34, 71.11, 1437.93, 2035.93, 1912.75]
```

---

### Repository

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 32.57 ns | 0.471 ns | 0.440 ns | 192 B |
| Imposter | 163.32 ns | 1.609 ns | 1.505 ns | 696 B |
| Mockolate | 72.96 ns | 0.618 ns | 0.578 ns | 456 B |
| Moq | 1,384.69 ns | 7.032 ns | 6.233 ns | 1912 B |
| NSubstitute | 2,009.77 ns | 19.169 ns | 16.993 ns | 5000 B |
| FakeItEasy | 1,914.33 ns | 26.091 ns | 24.405 ns | 2715 B |

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
  y-axis "Time (ns)" 0 --> 2412
  bar [32.57, 163.32, 72.96, 1384.69, 2009.77, 1914.33]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-11T03:26:37.062Z*
