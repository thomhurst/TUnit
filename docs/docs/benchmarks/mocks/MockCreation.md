---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 5
---

# MockCreation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock instance creation performance:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27.63 ns | 0.253 ns | 0.224 ns | 192 B |
| Imposter | 101.38 ns | 0.253 ns | 0.224 ns | 440 B |
| Mockolate | 71.58 ns | 0.188 ns | 0.157 ns | 384 B |
| Moq | 1,275.47 ns | 19.742 ns | 17.501 ns | 2048 B |
| NSubstitute | 1,768.84 ns | 9.955 ns | 9.312 ns | 5000 B |
| FakeItEasy | 1,692.52 ns | 32.779 ns | 32.193 ns | 2715 B |

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
  y-axis "Time (ns)" 0 --> 2123
  bar [27.63, 101.38, 71.58, 1275.47, 1768.84, 1692.52]
```

---

### Repository

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 27.11 ns | 0.606 ns | 0.567 ns | 192 B |
| Imposter | 158.61 ns | 0.751 ns | 0.703 ns | 696 B |
| Mockolate | 72.85 ns | 0.970 ns | 0.860 ns | 384 B |
| Moq | 1,239.28 ns | 9.312 ns | 7.776 ns | 1912 B |
| NSubstitute | 1,769.66 ns | 13.072 ns | 10.916 ns | 5000 B |
| FakeItEasy | 1,746.41 ns | 9.410 ns | 7.858 ns | 2715 B |

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
  y-axis "Time (ns)" 0 --> 2124
  bar [27.11, 158.61, 72.85, 1239.28, 1769.66, 1746.41]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-29T03:24:49.990Z*
