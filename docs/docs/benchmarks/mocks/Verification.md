---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 736.82 ns | 5.255 ns | 4.916 ns | 3008 B |
| Imposter | 710.31 ns | 9.659 ns | 8.563 ns | 4688 B |
| Mockolate | 440.54 ns | 5.016 ns | 4.692 ns | 2128 B |
| Moq | 347,214.28 ns | 1,820.099 ns | 1,702.521 ns | 24325 B |
| NSubstitute | 6,518.20 ns | 30.953 ns | 27.439 ns | 10064 B |
| FakeItEasy | 7,384.15 ns | 51.376 ns | 45.543 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 416658
  bar [736.82, 710.31, 440.54, 347214.28, 6518.2, 7384.15]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.84 ns | 0.834 ns | 0.739 ns | 320 B |
| Imposter | 338.58 ns | 5.520 ns | 5.164 ns | 2400 B |
| Mockolate | 250.82 ns | 2.885 ns | 2.698 ns | 1144 B |
| Moq | 89,641.96 ns | 410.959 ns | 343.169 ns | 6998 B |
| NSubstitute | 3,645.11 ns | 29.986 ns | 28.049 ns | 7088 B |
| FakeItEasy | 3,651.58 ns | 51.982 ns | 48.624 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107571
  bar [54.84, 338.58, 250.82, 89641.96, 3645.11, 3651.58]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,302.91 ns | 15.175 ns | 14.194 ns | 4472 B |
| Imposter | 1,868.83 ns | 24.628 ns | 23.037 ns | 11192 B |
| Mockolate | 1,169.26 ns | 4.031 ns | 3.147 ns | 5240 B |
| Moq | 478,220.92 ns | 3,698.679 ns | 3,459.746 ns | 34779 B |
| NSubstitute | 11,755.17 ns | 133.307 ns | 124.695 ns | 16763 B |
| FakeItEasy | 13,742.62 ns | 209.540 ns | 196.004 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 573866
  bar [1302.91, 1868.83, 1169.26, 478220.92, 11755.17, 13742.62]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-30T03:28:32.223Z*
