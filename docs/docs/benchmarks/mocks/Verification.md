---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 779.60 ns | 6.673 ns | 6.242 ns | 3000 B |
| Imposter | 690.03 ns | 7.838 ns | 6.948 ns | 4688 B |
| Mockolate | 416.06 ns | 2.350 ns | 2.199 ns | 2240 B |
| Moq | 244,642.07 ns | 1,445.522 ns | 1,281.418 ns | 24324 B |
| NSubstitute | 5,987.73 ns | 69.352 ns | 61.478 ns | 10064 B |
| FakeItEasy | 6,625.19 ns | 46.281 ns | 43.292 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 293571
  bar [779.6, 690.03, 416.06, 244642.07, 5987.73, 6625.19]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.49 ns | 0.231 ns | 0.216 ns | 312 B |
| Imposter | 329.23 ns | 1.555 ns | 1.298 ns | 2400 B |
| Mockolate | 258.48 ns | 1.748 ns | 1.635 ns | 1240 B |
| Moq | 64,461.44 ns | 533.105 ns | 498.667 ns | 7037 B |
| NSubstitute | 3,541.92 ns | 66.165 ns | 67.947 ns | 7088 B |
| FakeItEasy | 3,466.64 ns | 68.691 ns | 96.295 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 77354
  bar [56.49, 329.23, 258.48, 64461.44, 3541.92, 3466.64]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,305.30 ns | 10.695 ns | 8.930 ns | 4464 B |
| Imposter | 1,777.89 ns | 33.480 ns | 34.381 ns | 11192 B |
| Mockolate | 1,174.13 ns | 23.009 ns | 28.257 ns | 5376 B |
| Moq | 354,982.55 ns | 4,030.569 ns | 3,770.197 ns | 34699 B |
| NSubstitute | 11,088.49 ns | 54.167 ns | 45.232 ns | 16762 B |
| FakeItEasy | 12,204.31 ns | 121.898 ns | 114.023 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 425980
  bar [1305.3, 1777.89, 1174.13, 354982.55, 11088.49, 12204.31]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-11T03:26:37.062Z*
