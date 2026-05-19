---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 647.20 ns | 11.654 ns | 10.901 ns | 2864 B |
| Imposter | 697.68 ns | 13.458 ns | 13.218 ns | 4688 B |
| Mockolate | 418.84 ns | 8.284 ns | 12.650 ns | 2240 B |
| Moq | 343,584.41 ns | 2,931.627 ns | 2,448.041 ns | 24325 B |
| NSubstitute | 6,343.02 ns | 95.969 ns | 80.138 ns | 10064 B |
| FakeItEasy | 7,912.70 ns | 74.894 ns | 70.056 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 412302
  bar [647.2, 697.68, 418.84, 343584.41, 6343.02, 7912.7]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.78 ns | 1.043 ns | 1.242 ns | 304 B |
| Imposter | 328.29 ns | 6.616 ns | 11.054 ns | 2400 B |
| Mockolate | 232.08 ns | 2.634 ns | 2.200 ns | 1240 B |
| Moq | 87,091.87 ns | 531.055 ns | 496.749 ns | 6918 B |
| NSubstitute | 3,557.92 ns | 49.194 ns | 46.016 ns | 7088 B |
| FakeItEasy | 3,662.19 ns | 57.962 ns | 51.381 ns | 5324 B |

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
  y-axis "Time (ns)" 0 --> 104511
  bar [51.78, 328.29, 232.08, 87091.87, 3557.92, 3662.19]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,111.16 ns | 13.263 ns | 12.406 ns | 4176 B |
| Imposter | 1,767.78 ns | 34.914 ns | 52.257 ns | 11192 B |
| Mockolate | 1,073.42 ns | 3.006 ns | 2.665 ns | 5376 B |
| Moq | 474,184.79 ns | 3,141.037 ns | 2,622.908 ns | 34699 B |
| NSubstitute | 11,068.04 ns | 148.951 ns | 132.042 ns | 16763 B |
| FakeItEasy | 13,418.53 ns | 164.870 ns | 154.219 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569022
  bar [1111.16, 1767.78, 1073.42, 474184.79, 11068.04, 13418.53]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-19T03:26:57.825Z*
