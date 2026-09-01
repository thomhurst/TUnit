---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 704.07 ns | 6.401 ns | 5.674 ns | 3008 B |
| Imposter | 719.94 ns | 6.671 ns | 6.240 ns | 4688 B |
| Mockolate | 405.14 ns | 4.625 ns | 4.326 ns | 2128 B |
| Moq | 346,228.33 ns | 2,595.046 ns | 2,427.408 ns | 24325 B |
| NSubstitute | 6,922.66 ns | 41.090 ns | 36.425 ns | 10064 B |
| FakeItEasy | 7,553.05 ns | 123.205 ns | 115.246 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415474
  bar [704.07, 719.94, 405.14, 346228.33, 6922.66, 7553.05]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.00 ns | 0.992 ns | 0.975 ns | 320 B |
| Imposter | 382.61 ns | 7.565 ns | 11.999 ns | 2400 B |
| Mockolate | 249.88 ns | 3.210 ns | 2.846 ns | 1144 B |
| Moq | 88,933.39 ns | 682.204 ns | 638.134 ns | 6918 B |
| NSubstitute | 4,065.70 ns | 53.043 ns | 49.617 ns | 7088 B |
| FakeItEasy | 3,852.88 ns | 45.252 ns | 37.787 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106721
  bar [54, 382.61, 249.88, 88933.39, 4065.7, 3852.88]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,372.46 ns | 10.678 ns | 9.988 ns | 4472 B |
| Imposter | 2,055.94 ns | 24.021 ns | 21.294 ns | 11192 B |
| Mockolate | 1,167.13 ns | 19.325 ns | 44.403 ns | 5240 B |
| Moq | 480,600.10 ns | 4,165.126 ns | 3,692.276 ns | 34699 B |
| NSubstitute | 12,288.20 ns | 95.098 ns | 79.411 ns | 16763 B |
| FakeItEasy | 14,136.41 ns | 134.184 ns | 112.050 ns | 19457 B |

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
  y-axis "Time (ns)" 0 --> 576721
  bar [1372.46, 2055.94, 1167.13, 480600.1, 12288.2, 14136.41]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-01T02:34:33.391Z*
