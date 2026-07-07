---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 718.76 ns | 7.148 ns | 6.687 ns | 3008 B |
| Imposter | 674.59 ns | 6.476 ns | 5.741 ns | 4688 B |
| Mockolate | 419.47 ns | 5.460 ns | 5.107 ns | 2128 B |
| Moq | 340,742.25 ns | 1,704.014 ns | 1,593.936 ns | 24325 B |
| NSubstitute | 6,288.57 ns | 35.756 ns | 33.447 ns | 10064 B |
| FakeItEasy | 7,295.57 ns | 59.001 ns | 55.189 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 408891
  bar [718.76, 674.59, 419.47, 340742.25, 6288.57, 7295.57]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.14 ns | 0.782 ns | 0.732 ns | 320 B |
| Imposter | 324.76 ns | 3.164 ns | 2.642 ns | 2400 B |
| Mockolate | 231.34 ns | 2.665 ns | 2.493 ns | 1144 B |
| Moq | 88,212.51 ns | 648.000 ns | 606.139 ns | 6918 B |
| NSubstitute | 3,662.17 ns | 20.899 ns | 19.549 ns | 7088 B |
| FakeItEasy | 3,843.24 ns | 52.702 ns | 49.298 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 105856
  bar [53.14, 324.76, 231.34, 88212.51, 3662.17, 3843.24]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,262.84 ns | 9.874 ns | 9.236 ns | 4472 B |
| Imposter | 1,788.23 ns | 14.679 ns | 13.731 ns | 11192 B |
| Mockolate | 1,114.03 ns | 15.060 ns | 14.087 ns | 5240 B |
| Moq | 472,335.22 ns | 2,266.522 ns | 2,009.213 ns | 34986 B |
| NSubstitute | 11,645.99 ns | 99.376 ns | 92.957 ns | 16929 B |
| FakeItEasy | 14,327.04 ns | 211.136 ns | 197.497 ns | 19457 B |

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
  y-axis "Time (ns)" 0 --> 566803
  bar [1262.84, 1788.23, 1114.03, 472335.22, 11645.99, 14327.04]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-07T03:24:42.900Z*
