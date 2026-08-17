---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 748.85 ns | 5.787 ns | 5.130 ns | 3008 B |
| Imposter | 728.72 ns | 14.548 ns | 22.650 ns | 4688 B |
| Mockolate | 418.10 ns | 7.182 ns | 6.718 ns | 2128 B |
| Moq | 349,866.50 ns | 1,574.848 ns | 1,473.113 ns | 24325 B |
| NSubstitute | 7,085.63 ns | 61.141 ns | 51.055 ns | 10064 B |
| FakeItEasy | 7,712.31 ns | 133.490 ns | 124.867 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 419840
  bar [748.85, 728.72, 418.1, 349866.5, 7085.63, 7712.31]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 50.22 ns | 0.482 ns | 0.403 ns | 320 B |
| Imposter | 328.23 ns | 2.914 ns | 2.434 ns | 2400 B |
| Mockolate | 234.01 ns | 1.659 ns | 1.385 ns | 1144 B |
| Moq | 90,293.52 ns | 472.785 ns | 442.244 ns | 6918 B |
| NSubstitute | 3,738.67 ns | 23.582 ns | 18.411 ns | 7088 B |
| FakeItEasy | 3,542.60 ns | 56.157 ns | 49.781 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 108353
  bar [50.22, 328.23, 234.01, 90293.52, 3738.67, 3542.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,250.39 ns | 10.930 ns | 9.689 ns | 4472 B |
| Imposter | 1,822.01 ns | 4.968 ns | 4.404 ns | 11192 B |
| Mockolate | 1,048.45 ns | 8.133 ns | 7.608 ns | 5240 B |
| Moq | 477,042.14 ns | 4,811.969 ns | 4,501.119 ns | 34699 B |
| NSubstitute | 12,677.49 ns | 183.405 ns | 171.557 ns | 16762 B |
| FakeItEasy | 13,732.81 ns | 227.434 ns | 233.558 ns | 19314 B |

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
  y-axis "Time (ns)" 0 --> 572451
  bar [1250.39, 1822.01, 1048.45, 477042.14, 12677.49, 13732.81]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-17T02:43:20.076Z*
