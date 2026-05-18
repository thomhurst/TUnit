---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 688.23 ns | 2.625 ns | 2.327 ns | 2864 B |
| Imposter | 693.89 ns | 4.668 ns | 4.138 ns | 4688 B |
| Mockolate | 426.28 ns | 2.512 ns | 2.349 ns | 2240 B |
| Moq | 254,929.86 ns | 965.463 ns | 855.858 ns | 24675 B |
| NSubstitute | 5,929.76 ns | 38.266 ns | 35.794 ns | 10064 B |
| FakeItEasy | 6,360.66 ns | 33.079 ns | 30.942 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 305916
  bar [688.23, 693.89, 426.28, 254929.86, 5929.76, 6360.66]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.48 ns | 0.318 ns | 0.298 ns | 304 B |
| Imposter | 343.49 ns | 2.372 ns | 2.102 ns | 2400 B |
| Mockolate | 252.98 ns | 2.706 ns | 2.532 ns | 1240 B |
| Moq | 63,632.74 ns | 357.025 ns | 298.132 ns | 6925 B |
| NSubstitute | 3,474.70 ns | 23.488 ns | 19.614 ns | 7088 B |
| FakeItEasy | 3,334.04 ns | 33.249 ns | 31.101 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 76360
  bar [52.48, 343.49, 252.98, 63632.74, 3474.7, 3334.04]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,251.75 ns | 13.265 ns | 11.759 ns | 4176 B |
| Imposter | 1,730.02 ns | 33.558 ns | 92.989 ns | 11192 B |
| Mockolate | 1,240.19 ns | 25.117 ns | 74.059 ns | 5376 B |
| Moq | 348,362.29 ns | 3,175.818 ns | 2,970.662 ns | 34699 B |
| NSubstitute | 10,711.46 ns | 85.749 ns | 80.210 ns | 16762 B |
| FakeItEasy | 11,695.33 ns | 168.221 ns | 157.354 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 418035
  bar [1251.75, 1730.02, 1240.19, 348362.29, 10711.46, 11695.33]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-18T03:29:10.052Z*
