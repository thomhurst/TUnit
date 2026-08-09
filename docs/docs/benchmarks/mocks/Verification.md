---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 850.61 ns | 10.672 ns | 9.982 ns | 3008 B |
| Imposter | 883.33 ns | 16.989 ns | 18.178 ns | 4688 B |
| Mockolate | 482.71 ns | 5.530 ns | 4.902 ns | 2128 B |
| Moq | 360,656.91 ns | 3,261.115 ns | 2,723.179 ns | 24564 B |
| NSubstitute | 6,660.30 ns | 12.520 ns | 10.455 ns | 10064 B |
| FakeItEasy | 7,936.47 ns | 35.916 ns | 31.839 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 432789
  bar [850.61, 883.33, 482.71, 360656.91, 6660.3, 7936.47]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 62.53 ns | 1.069 ns | 1.000 ns | 320 B |
| Imposter | 383.58 ns | 7.612 ns | 7.120 ns | 2400 B |
| Mockolate | 258.60 ns | 5.092 ns | 6.621 ns | 1144 B |
| Moq | 90,970.40 ns | 364.060 ns | 340.542 ns | 6918 B |
| NSubstitute | 3,865.43 ns | 29.036 ns | 27.160 ns | 7088 B |
| FakeItEasy | 3,774.42 ns | 27.993 ns | 23.375 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 109165
  bar [62.53, 383.58, 258.6, 90970.4, 3865.43, 3774.42]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,318.74 ns | 15.285 ns | 14.298 ns | 4472 B |
| Imposter | 1,954.59 ns | 37.666 ns | 36.993 ns | 11192 B |
| Mockolate | 1,172.60 ns | 22.811 ns | 34.835 ns | 5240 B |
| Moq | 485,595.43 ns | 3,281.595 ns | 2,740.280 ns | 34811 B |
| NSubstitute | 11,747.82 ns | 112.006 ns | 99.290 ns | 16763 B |
| FakeItEasy | 14,345.40 ns | 209.651 ns | 196.108 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 582715
  bar [1318.74, 1954.59, 1172.6, 485595.43, 11747.82, 14345.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-09T03:02:07.270Z*
