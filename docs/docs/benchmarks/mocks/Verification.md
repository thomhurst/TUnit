---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 748.50 ns | 8.762 ns | 8.196 ns | 2968 B |
| Imposter | 697.96 ns | 8.708 ns | 8.145 ns | 4688 B |
| Mockolate | 409.36 ns | 4.167 ns | 3.898 ns | 2240 B |
| Moq | 244,163.13 ns | 1,858.746 ns | 1,738.672 ns | 24324 B |
| NSubstitute | 5,962.74 ns | 84.536 ns | 79.075 ns | 10064 B |
| FakeItEasy | 6,663.36 ns | 115.716 ns | 108.241 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 292996
  bar [748.5, 697.96, 409.36, 244163.13, 5962.74, 6663.36]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.69 ns | 0.430 ns | 0.402 ns | 304 B |
| Imposter | 330.54 ns | 6.382 ns | 5.658 ns | 2400 B |
| Mockolate | 246.75 ns | 2.633 ns | 2.463 ns | 1240 B |
| Moq | 63,093.83 ns | 340.587 ns | 284.406 ns | 6925 B |
| NSubstitute | 3,480.90 ns | 61.830 ns | 54.810 ns | 7088 B |
| FakeItEasy | 3,346.73 ns | 63.758 ns | 62.618 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75713
  bar [52.69, 330.54, 246.75, 63093.83, 3480.9, 3346.73]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,268.13 ns | 15.355 ns | 14.363 ns | 4384 B |
| Imposter | 1,800.60 ns | 35.585 ns | 39.552 ns | 11192 B |
| Mockolate | 1,240.47 ns | 18.198 ns | 14.208 ns | 5376 B |
| Moq | 350,159.67 ns | 3,407.180 ns | 3,020.376 ns | 34811 B |
| NSubstitute | 10,585.18 ns | 161.694 ns | 151.248 ns | 16762 B |
| FakeItEasy | 12,068.17 ns | 229.802 ns | 245.885 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 420192
  bar [1268.13, 1800.6, 1240.47, 350159.67, 10585.18, 12068.17]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-23T03:25:20.859Z*
