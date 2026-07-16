---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 765.52 ns | 15.180 ns | 14.200 ns | 3008 B |
| Imposter | 762.21 ns | 11.127 ns | 10.408 ns | 4688 B |
| Mockolate | 452.26 ns | 8.813 ns | 8.656 ns | 2128 B |
| Moq | 348,657.31 ns | 3,322.000 ns | 2,944.866 ns | 24325 B |
| NSubstitute | 6,630.49 ns | 33.134 ns | 29.372 ns | 10064 B |
| FakeItEasy | 7,724.77 ns | 85.834 ns | 76.089 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 418389
  bar [765.52, 762.21, 452.26, 348657.31, 6630.49, 7724.77]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.97 ns | 1.070 ns | 1.001 ns | 320 B |
| Imposter | 352.60 ns | 7.062 ns | 6.606 ns | 2400 B |
| Mockolate | 258.29 ns | 5.210 ns | 5.574 ns | 1144 B |
| Moq | 90,770.52 ns | 1,214.939 ns | 1,136.455 ns | 6918 B |
| NSubstitute | 3,939.88 ns | 39.475 ns | 34.994 ns | 7088 B |
| FakeItEasy | 3,848.23 ns | 76.703 ns | 67.996 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 108925
  bar [56.97, 352.6, 258.29, 90770.52, 3939.88, 3848.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,369.78 ns | 22.086 ns | 20.660 ns | 4472 B |
| Imposter | 1,946.82 ns | 33.126 ns | 30.986 ns | 11192 B |
| Mockolate | 1,224.99 ns | 24.039 ns | 32.904 ns | 5240 B |
| Moq | 481,850.05 ns | 1,620.866 ns | 1,353.496 ns | 34699 B |
| NSubstitute | 11,776.22 ns | 92.014 ns | 86.069 ns | 16929 B |
| FakeItEasy | 15,344.78 ns | 191.188 ns | 159.650 ns | 19458 B |

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
  y-axis "Time (ns)" 0 --> 578221
  bar [1369.78, 1946.82, 1224.99, 481850.05, 11776.22, 15344.78]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-16T03:22:07.543Z*
