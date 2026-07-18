---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 689.10 ns | 2.200 ns | 1.950 ns | 3008 B |
| Imposter | 684.66 ns | 6.232 ns | 5.830 ns | 4688 B |
| Mockolate | 406.49 ns | 1.674 ns | 1.307 ns | 2128 B |
| Moq | 347,635.64 ns | 3,176.632 ns | 2,652.631 ns | 24325 B |
| NSubstitute | 6,283.63 ns | 17.144 ns | 15.197 ns | 10064 B |
| FakeItEasy | 7,407.38 ns | 31.692 ns | 29.644 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417163
  bar [689.1, 684.66, 406.49, 347635.64, 6283.63, 7407.38]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.39 ns | 0.419 ns | 0.372 ns | 320 B |
| Imposter | 321.12 ns | 1.590 ns | 1.328 ns | 2400 B |
| Mockolate | 230.38 ns | 1.168 ns | 1.092 ns | 1144 B |
| Moq | 88,245.84 ns | 434.153 ns | 406.107 ns | 6918 B |
| NSubstitute | 3,653.04 ns | 10.605 ns | 9.920 ns | 7088 B |
| FakeItEasy | 3,781.94 ns | 54.384 ns | 50.870 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 105896
  bar [52.39, 321.12, 230.38, 88245.84, 3653.04, 3781.94]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,274.86 ns | 17.317 ns | 16.198 ns | 4472 B |
| Imposter | 1,818.39 ns | 36.213 ns | 33.874 ns | 11192 B |
| Mockolate | 1,161.97 ns | 20.433 ns | 18.113 ns | 5240 B |
| Moq | 483,704.83 ns | 2,577.177 ns | 2,284.601 ns | 34699 B |
| NSubstitute | 11,688.35 ns | 66.657 ns | 52.041 ns | 16929 B |
| FakeItEasy | 13,487.62 ns | 248.554 ns | 255.247 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 580446
  bar [1274.86, 1818.39, 1161.97, 483704.83, 11688.35, 13487.62]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-18T03:20:37.479Z*
