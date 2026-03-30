---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 896.90 ns | 9.579 ns | 8.492 ns | 3864 B |
| Imposter | 680.42 ns | 7.698 ns | 7.201 ns | 4688 B |
| Mockolate | 925.96 ns | 8.563 ns | 7.151 ns | 3168 B |
| Moq | 342,360.82 ns | 1,086.587 ns | 963.231 ns | 24325 B |
| NSubstitute | 6,357.02 ns | 34.062 ns | 30.195 ns | 10064 B |
| FakeItEasy | 7,241.56 ns | 58.115 ns | 54.361 ns | 10722 B |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 410833
  bar [896.9, 680.42, 925.96, 342360.82, 6357.02, 7241.56]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 71.42 ns | 0.648 ns | 0.575 ns | 392 B |
| Imposter | 315.24 ns | 3.601 ns | 3.192 ns | 2400 B |
| Mockolate | 208.51 ns | 2.545 ns | 2.256 ns | 904 B |
| Moq | 88,227.46 ns | 576.177 ns | 538.956 ns | 6918 B |
| NSubstitute | 3,563.20 ns | 19.444 ns | 17.236 ns | 7088 B |
| FakeItEasy | 3,598.38 ns | 31.962 ns | 29.897 ns | 5210 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 105873
  bar [71.42, 315.24, 208.51, 88227.46, 3563.2, 3598.38]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,667.08 ns | 19.635 ns | 17.406 ns | 5592 B |
| Imposter | 1,753.72 ns | 19.614 ns | 17.388 ns | 11192 B |
| Mockolate | 1,845.95 ns | 13.018 ns | 10.870 ns | 5592 B |
| Moq | 471,550.06 ns | 2,862.196 ns | 2,677.300 ns | 34954 B |
| NSubstitute | 11,569.30 ns | 226.488 ns | 222.441 ns | 16763 B |
| FakeItEasy | 13,677.73 ns | 147.432 ns | 130.695 ns | 19457 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 565861
  bar [1667.08, 1753.72, 1845.95, 471550.06, 11569.3, 13677.73]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-30T01:06:26.815Z*
