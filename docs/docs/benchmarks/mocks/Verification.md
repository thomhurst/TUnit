---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 777.76 ns | 5.452 ns | 5.099 ns | 3080 B |
| Imposter | 690.29 ns | 13.603 ns | 12.724 ns | 4688 B |
| Mockolate | 914.38 ns | 6.329 ns | 5.920 ns | 3152 B |
| Moq | 244,662.85 ns | 1,375.187 ns | 1,219.067 ns | 24324 B |
| NSubstitute | 5,935.51 ns | 37.598 ns | 33.330 ns | 10064 B |
| FakeItEasy | 6,426.41 ns | 44.979 ns | 37.559 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 293596
  bar [777.76, 690.29, 914.38, 244662.85, 5935.51, 6426.41]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 65.63 ns | 1.204 ns | 1.127 ns | 328 B |
| Imposter | 322.00 ns | 2.463 ns | 2.183 ns | 2400 B |
| Mockolate | 252.16 ns | 4.871 ns | 4.784 ns | 952 B |
| Moq | 63,049.50 ns | 739.885 ns | 692.089 ns | 6925 B |
| NSubstitute | 3,449.61 ns | 51.936 ns | 46.040 ns | 7088 B |
| FakeItEasy | 3,271.38 ns | 39.151 ns | 36.622 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75660
  bar [65.63, 322, 252.16, 63049.5, 3449.61, 3271.38]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,345.98 ns | 25.533 ns | 25.077 ns | 4608 B |
| Imposter | 1,708.36 ns | 25.316 ns | 23.681 ns | 11192 B |
| Mockolate | 1,811.35 ns | 15.759 ns | 13.160 ns | 5496 B |
| Moq | 351,601.41 ns | 4,212.651 ns | 3,734.406 ns | 34699 B |
| NSubstitute | 10,584.62 ns | 143.054 ns | 133.813 ns | 16762 B |
| FakeItEasy | 12,276.65 ns | 242.562 ns | 297.888 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 421922
  bar [1345.98, 1708.36, 1811.35, 351601.41, 10584.62, 12276.65]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-20T03:23:48.728Z*
