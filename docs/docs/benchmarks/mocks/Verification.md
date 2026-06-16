---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 635.78 ns | 4.345 ns | 3.852 ns | 3008 B |
| Imposter | 626.37 ns | 4.965 ns | 4.146 ns | 4688 B |
| Mockolate | 368.76 ns | 1.256 ns | 1.175 ns | 2240 B |
| Moq | 317,960.35 ns | 3,562.982 ns | 3,332.816 ns | 24325 B |
| NSubstitute | 5,713.78 ns | 54.002 ns | 50.514 ns | 10064 B |
| FakeItEasy | 6,844.92 ns | 42.896 ns | 38.026 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 381553
  bar [635.78, 626.37, 368.76, 317960.35, 5713.78, 6844.92]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.28 ns | 0.182 ns | 0.152 ns | 320 B |
| Imposter | 285.68 ns | 1.770 ns | 1.655 ns | 2400 B |
| Mockolate | 223.94 ns | 0.521 ns | 0.435 ns | 1240 B |
| Moq | 80,958.08 ns | 520.424 ns | 486.805 ns | 6918 B |
| NSubstitute | 3,226.29 ns | 6.706 ns | 5.236 ns | 7088 B |
| FakeItEasy | 3,436.39 ns | 32.473 ns | 27.117 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 97150
  bar [46.28, 285.68, 223.94, 80958.08, 3226.29, 3436.39]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,123.15 ns | 4.289 ns | 3.581 ns | 4472 B |
| Imposter | 1,568.51 ns | 7.513 ns | 6.660 ns | 11192 B |
| Mockolate | 1,048.82 ns | 6.147 ns | 5.750 ns | 5376 B |
| Moq | 433,476.81 ns | 3,003.389 ns | 2,662.426 ns | 34842 B |
| NSubstitute | 10,403.50 ns | 69.813 ns | 65.303 ns | 16762 B |
| FakeItEasy | 12,452.53 ns | 155.500 ns | 121.404 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 520173
  bar [1123.15, 1568.51, 1048.82, 433476.81, 10403.5, 12452.53]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-16T03:29:20.737Z*
