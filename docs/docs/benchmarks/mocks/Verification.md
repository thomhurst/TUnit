---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 779.15 ns | 15.178 ns | 22.248 ns | 3008 B |
| Imposter | 716.36 ns | 14.186 ns | 25.939 ns | 4688 B |
| Mockolate | 399.19 ns | 3.415 ns | 3.195 ns | 2128 B |
| Moq | 240,559.49 ns | 2,584.282 ns | 2,417.339 ns | 24324 B |
| NSubstitute | 5,900.53 ns | 53.457 ns | 50.003 ns | 10064 B |
| FakeItEasy | 6,571.55 ns | 127.863 ns | 119.603 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 288672
  bar [779.15, 716.36, 399.19, 240559.49, 5900.53, 6571.55]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.37 ns | 0.683 ns | 0.605 ns | 320 B |
| Imposter | 332.65 ns | 6.574 ns | 7.571 ns | 2400 B |
| Mockolate | 249.90 ns | 4.958 ns | 6.950 ns | 1144 B |
| Moq | 62,395.28 ns | 429.842 ns | 402.074 ns | 7069 B |
| NSubstitute | 3,608.01 ns | 71.981 ns | 85.688 ns | 7088 B |
| FakeItEasy | 3,358.47 ns | 62.317 ns | 83.191 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74875
  bar [56.37, 332.65, 249.9, 62395.28, 3608.01, 3358.47]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,316.45 ns | 26.207 ns | 31.197 ns | 4472 B |
| Imposter | 1,843.45 ns | 36.758 ns | 91.539 ns | 11192 B |
| Mockolate | 1,140.49 ns | 22.839 ns | 65.529 ns | 5240 B |
| Moq | 352,256.02 ns | 3,962.299 ns | 3,706.337 ns | 34699 B |
| NSubstitute | 11,016.18 ns | 98.963 ns | 82.638 ns | 16762 B |
| FakeItEasy | 12,404.47 ns | 115.001 ns | 101.945 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 422708
  bar [1316.45, 1843.45, 1140.49, 352256.02, 11016.18, 12404.47]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-11T02:59:33.302Z*
