---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 775.39 ns | 6.574 ns | 5.828 ns | 3008 B |
| Imposter | 703.00 ns | 13.987 ns | 30.109 ns | 4688 B |
| Mockolate | 427.02 ns | 8.485 ns | 8.334 ns | 2128 B |
| Moq | 351,928.62 ns | 3,126.655 ns | 2,924.675 ns | 24349 B |
| NSubstitute | 6,500.53 ns | 44.338 ns | 41.474 ns | 10064 B |
| FakeItEasy | 7,546.04 ns | 45.700 ns | 38.161 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 422315
  bar [775.39, 703, 427.02, 351928.62, 6500.53, 7546.04]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 55.16 ns | 1.125 ns | 1.105 ns | 320 B |
| Imposter | 326.57 ns | 6.564 ns | 12.167 ns | 2400 B |
| Mockolate | 243.33 ns | 4.801 ns | 4.491 ns | 1144 B |
| Moq | 88,267.52 ns | 959.286 ns | 801.047 ns | 6918 B |
| NSubstitute | 3,624.58 ns | 31.240 ns | 26.087 ns | 7088 B |
| FakeItEasy | 3,548.36 ns | 42.688 ns | 37.842 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 105922
  bar [55.16, 326.57, 243.33, 88267.52, 3624.58, 3548.36]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,241.06 ns | 11.009 ns | 9.759 ns | 4472 B |
| Imposter | 1,678.05 ns | 29.881 ns | 27.950 ns | 11192 B |
| Mockolate | 1,053.93 ns | 8.026 ns | 7.115 ns | 5240 B |
| Moq | 475,944.30 ns | 3,458.164 ns | 3,234.769 ns | 34699 B |
| NSubstitute | 11,070.80 ns | 39.862 ns | 35.336 ns | 16762 B |
| FakeItEasy | 13,892.43 ns | 87.028 ns | 72.672 ns | 19314 B |

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
  y-axis "Time (ns)" 0 --> 571134
  bar [1241.06, 1678.05, 1053.93, 475944.3, 11070.8, 13892.43]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-26T03:28:53.126Z*
