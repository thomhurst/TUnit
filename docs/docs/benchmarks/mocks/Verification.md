---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-10** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 742.31 ns | 10.522 ns | 9.328 ns | 3008 B |
| Imposter | 743.55 ns | 14.049 ns | 13.798 ns | 4688 B |
| Mockolate | 433.85 ns | 8.638 ns | 15.354 ns | 2128 B |
| Moq | 349,022.01 ns | 2,006.187 ns | 1,876.588 ns | 24325 B |
| NSubstitute | 6,649.64 ns | 42.325 ns | 39.591 ns | 10064 B |
| FakeItEasy | 8,090.37 ns | 95.559 ns | 89.386 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 418827
  bar [742.31, 743.55, 433.85, 349022.01, 6649.64, 8090.37]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 56.29 ns | 0.721 ns | 0.674 ns | 320 B |
| Imposter | 344.95 ns | 6.707 ns | 6.888 ns | 2400 B |
| Mockolate | 257.91 ns | 3.740 ns | 3.498 ns | 1144 B |
| Moq | 89,539.03 ns | 598.360 ns | 559.707 ns | 6918 B |
| NSubstitute | 3,683.70 ns | 18.854 ns | 17.636 ns | 7088 B |
| FakeItEasy | 3,730.76 ns | 55.657 ns | 52.061 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107447
  bar [56.29, 344.95, 257.91, 89539.03, 3683.7, 3730.76]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,309.78 ns | 19.387 ns | 18.134 ns | 4472 B |
| Imposter | 1,797.96 ns | 35.880 ns | 80.987 ns | 11192 B |
| Mockolate | 1,251.59 ns | 23.722 ns | 22.190 ns | 5240 B |
| Moq | 475,527.58 ns | 1,396.531 ns | 1,237.988 ns | 34699 B |
| NSubstitute | 11,547.33 ns | 91.641 ns | 81.237 ns | 16763 B |
| FakeItEasy | 13,953.41 ns | 208.477 ns | 195.010 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 570634
  bar [1309.78, 1797.96, 1251.59, 475527.58, 11547.33, 13953.41]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-10T03:24:43.056Z*
