---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 756.07 ns | 9.599 ns | 7.494 ns | 3080 B |
| Imposter | 688.82 ns | 5.003 ns | 4.435 ns | 4688 B |
| Mockolate | 929.28 ns | 13.545 ns | 12.670 ns | 3152 B |
| Moq | 251,281.81 ns | 2,632.162 ns | 2,333.343 ns | 24578 B |
| NSubstitute | 5,893.75 ns | 87.552 ns | 81.896 ns | 10064 B |
| FakeItEasy | 6,507.83 ns | 129.243 ns | 176.910 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 301539
  bar [756.07, 688.82, 929.28, 251281.81, 5893.75, 6507.83]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 64.01 ns | 1.248 ns | 1.225 ns | 328 B |
| Imposter | 326.35 ns | 3.848 ns | 3.600 ns | 2400 B |
| Mockolate | 230.45 ns | 1.082 ns | 0.845 ns | 952 B |
| Moq | 59,135.67 ns | 276.689 ns | 216.021 ns | 6925 B |
| NSubstitute | 3,342.08 ns | 16.167 ns | 14.332 ns | 7088 B |
| FakeItEasy | 3,179.63 ns | 18.132 ns | 15.141 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 70963
  bar [64.01, 326.35, 230.45, 59135.67, 3342.08, 3179.63]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,335.01 ns | 9.083 ns | 8.496 ns | 4608 B |
| Imposter | 1,704.49 ns | 16.312 ns | 15.258 ns | 11192 B |
| Mockolate | 1,902.74 ns | 7.040 ns | 5.879 ns | 5496 B |
| Moq | 351,910.14 ns | 3,212.409 ns | 3,004.889 ns | 34699 B |
| NSubstitute | 10,480.47 ns | 89.931 ns | 75.096 ns | 16889 B |
| FakeItEasy | 11,610.13 ns | 136.687 ns | 127.857 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 422293
  bar [1335.01, 1704.49, 1902.74, 351910.14, 10480.47, 11610.13]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-19T03:31:38.770Z*
