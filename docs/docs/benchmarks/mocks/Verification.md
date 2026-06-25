---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 717.33 ns | 5.995 ns | 5.608 ns | 3008 B |
| Imposter | 694.34 ns | 7.863 ns | 6.566 ns | 4688 B |
| Mockolate | 424.33 ns | 6.144 ns | 5.131 ns | 2128 B |
| Moq | 345,805.86 ns | 3,238.947 ns | 3,029.713 ns | 24325 B |
| NSubstitute | 6,298.41 ns | 50.186 ns | 41.908 ns | 10064 B |
| FakeItEasy | 7,396.73 ns | 30.830 ns | 24.070 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 414968
  bar [717.33, 694.34, 424.33, 345805.86, 6298.41, 7396.73]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.03 ns | 0.247 ns | 0.219 ns | 320 B |
| Imposter | 318.27 ns | 2.215 ns | 1.849 ns | 2400 B |
| Mockolate | 225.25 ns | 0.718 ns | 0.600 ns | 1144 B |
| Moq | 89,959.65 ns | 517.449 ns | 458.705 ns | 6918 B |
| NSubstitute | 3,547.08 ns | 18.333 ns | 15.309 ns | 7088 B |
| FakeItEasy | 3,632.14 ns | 37.650 ns | 35.218 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107952
  bar [53.03, 318.27, 225.25, 89959.65, 3547.08, 3632.14]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,241.45 ns | 6.445 ns | 6.028 ns | 4472 B |
| Imposter | 1,683.36 ns | 6.585 ns | 5.838 ns | 11192 B |
| Mockolate | 1,106.67 ns | 4.090 ns | 3.826 ns | 5240 B |
| Moq | 475,341.88 ns | 2,262.754 ns | 2,116.581 ns | 34699 B |
| NSubstitute | 11,324.97 ns | 44.515 ns | 39.462 ns | 16762 B |
| FakeItEasy | 13,568.19 ns | 161.338 ns | 143.022 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 570411
  bar [1241.45, 1683.36, 1106.67, 475341.88, 11324.97, 13568.19]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-25T03:27:42.911Z*
