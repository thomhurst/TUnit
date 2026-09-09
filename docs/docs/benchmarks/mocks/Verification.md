---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 652.46 ns | 10.193 ns | 9.534 ns | 3008 B |
| Imposter | 584.74 ns | 7.655 ns | 7.161 ns | 4688 B |
| Mockolate | 374.73 ns | 2.841 ns | 2.218 ns | 2128 B |
| Moq | 153,096.50 ns | 1,288.007 ns | 1,141.785 ns | 24338 B |
| NSubstitute | 5,835.27 ns | 72.006 ns | 67.354 ns | 10064 B |
| FakeItEasy | 4,767.53 ns | 72.157 ns | 67.496 ns | 10717 B |

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
  y-axis "Time (ns)" 0 --> 183716
  bar [652.46, 584.74, 374.73, 153096.5, 5835.27, 4767.53]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.69 ns | 0.924 ns | 1.886 ns | 320 B |
| Imposter | 264.01 ns | 5.251 ns | 6.047 ns | 2400 B |
| Mockolate | 207.27 ns | 2.957 ns | 2.766 ns | 1144 B |
| Moq | 38,838.68 ns | 376.239 ns | 351.934 ns | 6922 B |
| NSubstitute | 2,807.98 ns | 23.409 ns | 20.751 ns | 7088 B |
| FakeItEasy | 2,200.81 ns | 40.571 ns | 35.965 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 46607
  bar [46.69, 264.01, 207.27, 38838.68, 2807.98, 2200.81]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,095.53 ns | 3.653 ns | 3.050 ns | 4472 B |
| Imposter | 1,369.56 ns | 6.197 ns | 5.797 ns | 11192 B |
| Mockolate | 930.42 ns | 16.259 ns | 14.413 ns | 5240 B |
| Moq | 192,937.49 ns | 1,588.539 ns | 1,408.199 ns | 34584 B |
| NSubstitute | 10,414.72 ns | 199.925 ns | 222.216 ns | 16889 B |
| FakeItEasy | 8,451.57 ns | 144.920 ns | 113.144 ns | 19246 B |

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
  y-axis "Time (ns)" 0 --> 231525
  bar [1095.53, 1369.56, 930.42, 192937.49, 10414.72, 8451.57]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-09T02:32:56.707Z*
