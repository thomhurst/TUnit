---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 716.00 ns | 13.844 ns | 14.217 ns | 3000 B |
| Imposter | 773.07 ns | 8.710 ns | 8.147 ns | 4688 B |
| Mockolate | 439.18 ns | 4.338 ns | 3.623 ns | 2240 B |
| Moq | 342,700.61 ns | 2,166.091 ns | 1,920.183 ns | 24325 B |
| NSubstitute | 6,453.89 ns | 46.720 ns | 41.416 ns | 10064 B |
| FakeItEasy | 7,761.64 ns | 45.742 ns | 42.787 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411241
  bar [716, 773.07, 439.18, 342700.61, 6453.89, 7761.64]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.43 ns | 0.471 ns | 0.393 ns | 312 B |
| Imposter | 407.80 ns | 3.087 ns | 2.737 ns | 2400 B |
| Mockolate | 250.35 ns | 3.377 ns | 2.993 ns | 1240 B |
| Moq | 89,338.26 ns | 304.900 ns | 254.605 ns | 6918 B |
| NSubstitute | 3,818.12 ns | 71.926 ns | 67.279 ns | 7088 B |
| FakeItEasy | 3,672.98 ns | 54.025 ns | 47.892 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 107206
  bar [52.43, 407.8, 250.35, 89338.26, 3818.12, 3672.98]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,340.29 ns | 26.171 ns | 24.480 ns | 4464 B |
| Imposter | 1,966.11 ns | 25.942 ns | 24.266 ns | 11192 B |
| Mockolate | 1,223.26 ns | 20.543 ns | 19.216 ns | 5376 B |
| Moq | 480,641.60 ns | 3,038.949 ns | 2,693.949 ns | 34699 B |
| NSubstitute | 11,987.72 ns | 22.776 ns | 20.190 ns | 16762 B |
| FakeItEasy | 15,169.08 ns | 152.836 ns | 142.962 ns | 19314 B |

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
  y-axis "Time (ns)" 0 --> 576770
  bar [1340.29, 1966.11, 1223.26, 480641.6, 11987.72, 15169.08]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-09T03:29:02.106Z*
