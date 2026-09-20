---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 747.85 ns | 4.303 ns | 3.814 ns | 3008 B |
| Imposter | 688.13 ns | 3.716 ns | 3.103 ns | 4688 B |
| Mockolate | 391.02 ns | 2.018 ns | 1.888 ns | 2128 B |
| Moq | 249,934.43 ns | 1,689.783 ns | 1,497.948 ns | 24324 B |
| NSubstitute | 6,537.30 ns | 30.224 ns | 25.238 ns | 10064 B |
| FakeItEasy | 6,559.09 ns | 70.935 ns | 62.882 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 299922
  bar [747.85, 688.13, 391.02, 249934.43, 6537.3, 6559.09]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.28 ns | 0.101 ns | 0.090 ns | 320 B |
| Imposter | 343.90 ns | 5.856 ns | 6.014 ns | 2400 B |
| Mockolate | 237.08 ns | 1.206 ns | 0.942 ns | 1144 B |
| Moq | 61,125.35 ns | 224.591 ns | 199.094 ns | 6925 B |
| NSubstitute | 3,563.81 ns | 10.550 ns | 8.810 ns | 7088 B |
| FakeItEasy | 3,217.19 ns | 9.188 ns | 7.174 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 73351
  bar [54.28, 343.9, 237.08, 61125.35, 3563.81, 3217.19]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,289.88 ns | 8.488 ns | 7.088 ns | 4472 B |
| Imposter | 1,709.45 ns | 16.197 ns | 14.358 ns | 11192 B |
| Mockolate | 1,055.19 ns | 4.540 ns | 4.247 ns | 5240 B |
| Moq | 338,589.99 ns | 1,416.219 ns | 1,105.691 ns | 34699 B |
| NSubstitute | 11,050.31 ns | 26.147 ns | 20.414 ns | 16762 B |
| FakeItEasy | 11,462.44 ns | 73.547 ns | 68.796 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 406308
  bar [1289.88, 1709.45, 1055.19, 338589.99, 11050.31, 11462.44]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-20T02:32:50.293Z*
