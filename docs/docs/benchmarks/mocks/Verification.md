---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 736.15 ns | 5.788 ns | 5.131 ns | 3008 B |
| Imposter | 761.17 ns | 9.846 ns | 8.729 ns | 4688 B |
| Mockolate | 438.75 ns | 4.658 ns | 3.889 ns | 2128 B |
| Moq | 356,042.34 ns | 1,619.002 ns | 1,514.416 ns | 24325 B |
| NSubstitute | 6,646.23 ns | 66.090 ns | 61.821 ns | 10064 B |
| FakeItEasy | 7,981.79 ns | 68.488 ns | 64.064 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 427251
  bar [736.15, 761.17, 438.75, 356042.34, 6646.23, 7981.79]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 59.04 ns | 0.686 ns | 0.641 ns | 320 B |
| Imposter | 369.10 ns | 6.834 ns | 6.058 ns | 2400 B |
| Mockolate | 254.06 ns | 2.347 ns | 2.196 ns | 1144 B |
| Moq | 90,094.40 ns | 388.460 ns | 363.366 ns | 6918 B |
| NSubstitute | 3,801.56 ns | 23.982 ns | 18.724 ns | 7088 B |
| FakeItEasy | 3,714.54 ns | 43.490 ns | 40.681 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 108114
  bar [59.04, 369.1, 254.06, 90094.4, 3801.56, 3714.54]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,299.54 ns | 18.393 ns | 17.205 ns | 4472 B |
| Imposter | 1,859.96 ns | 11.517 ns | 10.773 ns | 11192 B |
| Mockolate | 1,225.45 ns | 22.143 ns | 20.713 ns | 5240 B |
| Moq | 489,597.55 ns | 5,184.724 ns | 4,849.794 ns | 34699 B |
| NSubstitute | 12,001.81 ns | 61.037 ns | 50.969 ns | 16929 B |
| FakeItEasy | 14,040.77 ns | 203.349 ns | 169.805 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 587518
  bar [1299.54, 1859.96, 1225.45, 489597.55, 12001.81, 14040.77]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-01T03:21:53.196Z*
