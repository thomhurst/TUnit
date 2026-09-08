---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 720.66 ns | 6.668 ns | 6.238 ns | 3008 B |
| Imposter | 695.97 ns | 10.325 ns | 9.658 ns | 4688 B |
| Mockolate | 400.70 ns | 2.111 ns | 1.975 ns | 2128 B |
| Moq | 243,020.67 ns | 1,117.495 ns | 990.630 ns | 24324 B |
| NSubstitute | 6,425.99 ns | 76.669 ns | 71.716 ns | 10064 B |
| FakeItEasy | 6,431.31 ns | 40.431 ns | 37.819 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 291625
  bar [720.66, 695.97, 400.7, 243020.67, 6425.99, 6431.31]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.26 ns | 0.980 ns | 0.916 ns | 320 B |
| Imposter | 322.45 ns | 1.027 ns | 0.911 ns | 2400 B |
| Mockolate | 239.36 ns | 0.593 ns | 0.555 ns | 1144 B |
| Moq | 61,410.23 ns | 342.675 ns | 286.149 ns | 6925 B |
| NSubstitute | 3,543.25 ns | 8.277 ns | 7.337 ns | 7088 B |
| FakeItEasy | 3,198.46 ns | 20.752 ns | 18.396 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 73693
  bar [60.26, 322.45, 239.36, 61410.23, 3543.25, 3198.46]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,271.90 ns | 4.787 ns | 4.244 ns | 4472 B |
| Imposter | 1,664.66 ns | 4.586 ns | 4.065 ns | 11192 B |
| Mockolate | 1,152.22 ns | 3.417 ns | 3.029 ns | 5240 B |
| Moq | 346,196.73 ns | 3,188.337 ns | 2,826.378 ns | 34699 B |
| NSubstitute | 11,239.48 ns | 31.074 ns | 27.546 ns | 16762 B |
| FakeItEasy | 11,466.49 ns | 95.406 ns | 84.575 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 415437
  bar [1271.9, 1664.66, 1152.22, 346196.73, 11239.48, 11466.49]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-08T02:32:39.573Z*
