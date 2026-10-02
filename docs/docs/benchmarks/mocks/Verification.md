---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-10-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 734.13 ns | 4.855 ns | 4.303 ns | 3024 B |
| Imposter | 708.53 ns | 6.146 ns | 5.749 ns | 4688 B |
| Mockolate | 405.03 ns | 4.323 ns | 3.610 ns | 2128 B |
| Moq | 349,278.41 ns | 3,295.970 ns | 3,083.052 ns | 24325 B |
| NSubstitute | 7,086.98 ns | 24.377 ns | 21.609 ns | 10176 B |
| FakeItEasy | 7,505.15 ns | 45.355 ns | 42.425 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 419135
  bar [734.13, 708.53, 405.03, 349278.41, 7086.98, 7505.15]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.06 ns | 0.575 ns | 0.537 ns | 320 B |
| Imposter | 323.03 ns | 3.014 ns | 2.819 ns | 2400 B |
| Mockolate | 234.68 ns | 4.567 ns | 4.886 ns | 1144 B |
| Moq | 89,788.84 ns | 312.778 ns | 277.269 ns | 6918 B |
| NSubstitute | 3,975.00 ns | 14.003 ns | 13.099 ns | 7088 B |
| FakeItEasy | 3,575.98 ns | 33.243 ns | 31.095 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107747
  bar [52.06, 323.03, 234.68, 89788.84, 3975, 3575.98]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,246.26 ns | 24.900 ns | 32.377 ns | 4512 B |
| Imposter | 1,744.64 ns | 15.516 ns | 13.755 ns | 11192 B |
| Mockolate | 1,078.66 ns | 8.550 ns | 7.139 ns | 5240 B |
| Moq | 474,689.95 ns | 2,413.732 ns | 2,015.575 ns | 34922 B |
| NSubstitute | 12,505.45 ns | 58.898 ns | 49.182 ns | 16763 B |
| FakeItEasy | 13,332.57 ns | 180.519 ns | 160.025 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569628
  bar [1246.26, 1744.64, 1078.66, 474689.95, 12505.45, 13332.57]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-10-02T02:44:13.542Z*
