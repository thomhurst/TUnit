---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 680.02 ns | 2.757 ns | 2.579 ns | 3008 B |
| Imposter | 683.29 ns | 4.951 ns | 4.631 ns | 4688 B |
| Mockolate | 401.22 ns | 2.587 ns | 2.161 ns | 2128 B |
| Moq | 336,822.14 ns | 2,322.205 ns | 2,172.192 ns | 24325 B |
| NSubstitute | 6,038.47 ns | 31.975 ns | 24.964 ns | 10064 B |
| FakeItEasy | 7,256.87 ns | 27.329 ns | 24.226 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 404187
  bar [680.02, 683.29, 401.22, 336822.14, 6038.47, 7256.87]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.05 ns | 0.442 ns | 0.413 ns | 320 B |
| Imposter | 315.15 ns | 2.723 ns | 2.547 ns | 2400 B |
| Mockolate | 230.04 ns | 1.461 ns | 1.367 ns | 1144 B |
| Moq | 85,824.47 ns | 389.291 ns | 345.096 ns | 6918 B |
| NSubstitute | 3,522.86 ns | 20.717 ns | 18.365 ns | 7088 B |
| FakeItEasy | 3,474.74 ns | 34.922 ns | 30.958 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 102990
  bar [51.05, 315.15, 230.04, 85824.47, 3522.86, 3474.74]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,518.94 ns | 7.646 ns | 7.152 ns | 4472 B |
| Imposter | 1,742.50 ns | 12.948 ns | 12.111 ns | 11192 B |
| Mockolate | 1,070.31 ns | 9.150 ns | 8.559 ns | 5240 B |
| Moq | 466,710.00 ns | 2,677.442 ns | 2,373.482 ns | 34699 B |
| NSubstitute | 11,311.02 ns | 49.199 ns | 43.614 ns | 16762 B |
| FakeItEasy | 13,735.68 ns | 163.002 ns | 127.261 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 560052
  bar [1518.94, 1742.5, 1070.31, 466710, 11311.02, 13735.68]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-28T03:20:43.557Z*
