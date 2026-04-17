---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 779.75 ns | 14.522 ns | 13.584 ns | 3080 B |
| Imposter | 697.99 ns | 12.340 ns | 14.211 ns | 4688 B |
| Mockolate | 923.67 ns | 13.586 ns | 12.708 ns | 3152 B |
| Moq | 246,002.68 ns | 2,007.103 ns | 1,877.445 ns | 24675 B |
| NSubstitute | 6,025.40 ns | 116.063 ns | 124.186 ns | 10064 B |
| FakeItEasy | 6,484.46 ns | 96.148 ns | 80.288 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 295204
  bar [779.75, 697.99, 923.67, 246002.68, 6025.4, 6484.46]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.57 ns | 1.083 ns | 1.013 ns | 328 B |
| Imposter | 339.67 ns | 4.611 ns | 4.087 ns | 2400 B |
| Mockolate | 244.38 ns | 3.407 ns | 2.845 ns | 952 B |
| Moq | 63,280.47 ns | 469.889 ns | 439.534 ns | 7037 B |
| NSubstitute | 3,612.39 ns | 70.100 ns | 95.954 ns | 7088 B |
| FakeItEasy | 3,636.87 ns | 22.107 ns | 18.461 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75937
  bar [60.57, 339.67, 244.38, 63280.47, 3612.39, 3636.87]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,391.12 ns | 9.208 ns | 8.163 ns | 4608 B |
| Imposter | 1,934.79 ns | 36.515 ns | 34.157 ns | 11192 B |
| Mockolate | 1,969.13 ns | 28.229 ns | 25.024 ns | 5496 B |
| Moq | 354,693.14 ns | 3,496.978 ns | 3,271.075 ns | 34699 B |
| NSubstitute | 11,172.45 ns | 70.593 ns | 66.032 ns | 16763 B |
| FakeItEasy | 13,089.09 ns | 99.377 ns | 88.095 ns | 19568 B |

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
  y-axis "Time (ns)" 0 --> 425632
  bar [1391.12, 1934.79, 1969.13, 354693.14, 11172.45, 13089.09]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-17T03:23:50.633Z*
