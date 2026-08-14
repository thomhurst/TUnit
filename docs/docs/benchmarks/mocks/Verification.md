---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 758.72 ns | 1.443 ns | 1.280 ns | 3008 B |
| Imposter | 679.02 ns | 5.394 ns | 4.781 ns | 4688 B |
| Mockolate | 397.41 ns | 1.650 ns | 1.377 ns | 2128 B |
| Moq | 245,563.37 ns | 1,216.103 ns | 1,078.043 ns | 24324 B |
| NSubstitute | 6,397.46 ns | 11.773 ns | 9.191 ns | 10064 B |
| FakeItEasy | 6,404.63 ns | 37.728 ns | 35.291 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 294677
  bar [758.72, 679.02, 397.41, 245563.37, 6397.46, 6404.63]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 55.62 ns | 0.234 ns | 0.208 ns | 320 B |
| Imposter | 326.62 ns | 2.874 ns | 2.689 ns | 2400 B |
| Mockolate | 242.68 ns | 0.964 ns | 0.752 ns | 1144 B |
| Moq | 62,044.29 ns | 701.427 ns | 656.115 ns | 6925 B |
| NSubstitute | 3,744.85 ns | 25.452 ns | 23.807 ns | 7088 B |
| FakeItEasy | 3,275.73 ns | 35.111 ns | 31.125 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74454
  bar [55.62, 326.62, 242.68, 62044.29, 3744.85, 3275.73]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,289.92 ns | 17.183 ns | 16.073 ns | 4472 B |
| Imposter | 1,780.71 ns | 20.835 ns | 17.398 ns | 11192 B |
| Mockolate | 1,111.56 ns | 16.961 ns | 15.035 ns | 5240 B |
| Moq | 352,596.74 ns | 3,281.538 ns | 3,069.553 ns | 34699 B |
| NSubstitute | 11,782.11 ns | 189.935 ns | 218.729 ns | 16889 B |
| FakeItEasy | 11,703.67 ns | 112.297 ns | 99.548 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 423117
  bar [1289.92, 1780.71, 1111.56, 352596.74, 11782.11, 11703.67]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-14T03:10:39.371Z*
