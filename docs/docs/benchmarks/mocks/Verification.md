---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 642.77 ns | 8.884 ns | 7.419 ns | 2864 B |
| Imposter | 836.56 ns | 16.614 ns | 34.311 ns | 4688 B |
| Mockolate | 405.43 ns | 7.993 ns | 15.400 ns | 2224 B |
| Moq | 337,957.52 ns | 1,691.715 ns | 1,499.661 ns | 24325 B |
| NSubstitute | 6,215.39 ns | 41.262 ns | 34.456 ns | 10064 B |
| FakeItEasy | 7,463.26 ns | 130.864 ns | 122.410 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 405550
  bar [642.77, 836.56, 405.43, 337957.52, 6215.39, 7463.26]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.13 ns | 0.785 ns | 0.696 ns | 304 B |
| Imposter | 320.12 ns | 4.573 ns | 4.277 ns | 2400 B |
| Mockolate | 240.68 ns | 3.390 ns | 3.171 ns | 1240 B |
| Moq | 86,020.50 ns | 343.409 ns | 286.762 ns | 6918 B |
| NSubstitute | 3,518.15 ns | 23.293 ns | 21.788 ns | 7088 B |
| FakeItEasy | 3,783.65 ns | 73.674 ns | 90.479 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103225
  bar [49.13, 320.12, 240.68, 86020.5, 3518.15, 3783.65]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,144.61 ns | 11.875 ns | 10.527 ns | 4176 B |
| Imposter | 1,732.59 ns | 34.075 ns | 31.873 ns | 11192 B |
| Mockolate | 1,164.21 ns | 11.636 ns | 10.315 ns | 5408 B |
| Moq | 466,365.74 ns | 3,065.300 ns | 2,867.283 ns | 34811 B |
| NSubstitute | 11,674.19 ns | 108.633 ns | 101.615 ns | 16929 B |
| FakeItEasy | 12,922.28 ns | 227.018 ns | 201.246 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 559639
  bar [1144.61, 1732.59, 1164.21, 466365.74, 11674.19, 12922.28]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-03T03:31:53.295Z*
