---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 707.81 ns | 12.580 ns | 11.152 ns | 3008 B |
| Imposter | 703.16 ns | 14.075 ns | 13.166 ns | 4688 B |
| Mockolate | 400.61 ns | 5.858 ns | 5.193 ns | 2128 B |
| Moq | 352,844.43 ns | 2,129.145 ns | 1,887.432 ns | 24325 B |
| NSubstitute | 6,489.29 ns | 68.310 ns | 63.897 ns | 10064 B |
| FakeItEasy | 7,250.42 ns | 23.094 ns | 18.031 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 423414
  bar [707.81, 703.16, 400.61, 352844.43, 6489.29, 7250.42]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.11 ns | 0.664 ns | 0.589 ns | 320 B |
| Imposter | 327.50 ns | 4.685 ns | 3.912 ns | 2400 B |
| Mockolate | 238.44 ns | 1.725 ns | 1.440 ns | 1144 B |
| Moq | 89,481.37 ns | 744.164 ns | 659.682 ns | 6918 B |
| NSubstitute | 3,686.23 ns | 27.714 ns | 25.924 ns | 7088 B |
| FakeItEasy | 3,739.49 ns | 36.919 ns | 32.728 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107378
  bar [53.11, 327.5, 238.44, 89481.37, 3686.23, 3739.49]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,243.28 ns | 12.288 ns | 11.494 ns | 4472 B |
| Imposter | 1,731.31 ns | 25.897 ns | 24.225 ns | 11192 B |
| Mockolate | 1,136.70 ns | 11.847 ns | 9.893 ns | 5240 B |
| Moq | 478,455.05 ns | 1,369.514 ns | 1,143.606 ns | 34842 B |
| NSubstitute | 11,378.50 ns | 137.956 ns | 122.294 ns | 16763 B |
| FakeItEasy | 14,544.69 ns | 282.933 ns | 336.811 ns | 19393 B |

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
  y-axis "Time (ns)" 0 --> 574147
  bar [1243.28, 1731.31, 1136.7, 478455.05, 11378.5, 14544.69]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-27T03:27:29.619Z*
