---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 620.94 ns | 7.499 ns | 7.015 ns | 3080 B |
| Imposter | 548.88 ns | 9.613 ns | 8.992 ns | 4688 B |
| Mockolate | 734.24 ns | 12.340 ns | 10.939 ns | 3152 B |
| Moq | 189,070.37 ns | 1,099.319 ns | 1,028.304 ns | 24324 B |
| NSubstitute | 4,553.21 ns | 57.996 ns | 54.249 ns | 10064 B |
| FakeItEasy | 5,159.34 ns | 73.304 ns | 64.982 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 226885
  bar [620.94, 548.88, 734.24, 189070.37, 4553.21, 5159.34]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.34 ns | 0.878 ns | 0.821 ns | 328 B |
| Imposter | 254.96 ns | 0.967 ns | 0.905 ns | 2400 B |
| Mockolate | 183.21 ns | 0.964 ns | 0.902 ns | 952 B |
| Moq | 48,576.30 ns | 297.077 ns | 248.073 ns | 6925 B |
| NSubstitute | 2,706.91 ns | 52.374 ns | 48.991 ns | 7088 B |
| FakeItEasy | 2,607.87 ns | 29.230 ns | 25.912 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 58292
  bar [52.34, 254.96, 183.21, 48576.3, 2706.91, 2607.87]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,072.14 ns | 19.938 ns | 18.650 ns | 4608 B |
| Imposter | 1,402.02 ns | 26.998 ns | 26.516 ns | 11192 B |
| Mockolate | 1,558.29 ns | 28.273 ns | 26.446 ns | 5496 B |
| Moq | 272,609.42 ns | 1,997.941 ns | 1,668.372 ns | 34699 B |
| NSubstitute | 8,230.96 ns | 132.991 ns | 124.400 ns | 16762 B |
| FakeItEasy | 9,231.25 ns | 184.102 ns | 232.831 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 327132
  bar [1072.14, 1402.02, 1558.29, 272609.42, 8230.96, 9231.25]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-23T03:25:34.373Z*
