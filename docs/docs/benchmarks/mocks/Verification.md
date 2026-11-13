---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-11-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 637.29 ns | 6.279 ns | 5.874 ns | 3040 B |
| Imposter | 242.59 ns | 1.526 ns | 1.427 ns | 1096 B |
| Mockolate | 365.73 ns | 4.376 ns | 4.094 ns | 2088 B |
| Moq | 156,543.96 ns | 1,354.097 ns | 1,266.623 ns | 24314 B |
| NSubstitute | 5,620.25 ns | 90.115 ns | 79.885 ns | 10024 B |
| FakeItEasy | 4,485.99 ns | 59.503 ns | 52.748 ns | 10501 B |

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
  y-axis "Time (ns)" 0 --> 187853
  bar [637.29, 242.59, 365.73, 156543.96, 5620.25, 4485.99]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 46.95 ns | 0.383 ns | 0.339 ns | 336 B |
| Imposter | 121.97 ns | 0.540 ns | 0.451 ns | 616 B |
| Mockolate | 205.11 ns | 2.219 ns | 2.076 ns | 1096 B |
| Moq | 38,388.62 ns | 250.113 ns | 233.956 ns | 6914 B |
| NSubstitute | 2,693.79 ns | 42.354 ns | 39.618 ns | 7056 B |
| FakeItEasy | 2,155.51 ns | 43.099 ns | 35.990 ns | 5118 B |

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
  y-axis "Time (ns)" 0 --> 46067
  bar [46.95, 121.97, 205.11, 38388.62, 2693.79, 2155.51]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,093.63 ns | 10.350 ns | 9.175 ns | 4528 B |
| Imposter | 490.46 ns | 7.657 ns | 7.163 ns | 2288 B |
| Mockolate | 899.58 ns | 10.862 ns | 9.628 ns | 5128 B |
| Moq | 201,293.94 ns | 824.376 ns | 771.122 ns | 34959 B |
| NSubstitute | 9,559.91 ns | 105.015 ns | 98.231 ns | 16633 B |
| FakeItEasy | 7,907.24 ns | 75.058 ns | 66.537 ns | 18715 B |

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
  y-axis "Time (ns)" 0 --> 241553
  bar [1093.63, 490.46, 899.58, 201293.94, 9559.91, 7907.24]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-11-13T02:20:37.421Z*
