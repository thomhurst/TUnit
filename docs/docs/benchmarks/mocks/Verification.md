---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 724.69 ns | 6.212 ns | 5.507 ns | 3008 B |
| Imposter | 724.41 ns | 13.847 ns | 16.484 ns | 4688 B |
| Mockolate | 419.73 ns | 8.098 ns | 17.775 ns | 2128 B |
| Moq | 341,924.83 ns | 1,175.323 ns | 1,041.894 ns | 24325 B |
| NSubstitute | 6,242.97 ns | 109.881 ns | 102.783 ns | 10064 B |
| FakeItEasy | 8,025.86 ns | 118.597 ns | 110.935 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 410310
  bar [724.69, 724.41, 419.73, 341924.83, 6242.97, 8025.86]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.93 ns | 0.805 ns | 0.753 ns | 320 B |
| Imposter | 322.24 ns | 2.511 ns | 2.226 ns | 2400 B |
| Mockolate | 239.04 ns | 4.695 ns | 7.027 ns | 1144 B |
| Moq | 86,996.03 ns | 575.278 ns | 509.969 ns | 6918 B |
| NSubstitute | 3,659.54 ns | 56.806 ns | 53.137 ns | 7088 B |
| FakeItEasy | 3,529.53 ns | 47.174 ns | 41.819 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104396
  bar [51.93, 322.24, 239.04, 86996.03, 3659.54, 3529.53]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,303.29 ns | 25.231 ns | 33.683 ns | 4472 B |
| Imposter | 1,788.82 ns | 33.485 ns | 55.016 ns | 11192 B |
| Mockolate | 1,234.93 ns | 24.624 ns | 36.856 ns | 5240 B |
| Moq | 480,273.51 ns | 3,394.557 ns | 3,009.186 ns | 34986 B |
| NSubstitute | 11,633.36 ns | 81.906 ns | 76.615 ns | 16762 B |
| FakeItEasy | 14,510.31 ns | 215.501 ns | 191.036 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 576329
  bar [1303.29, 1788.82, 1234.93, 480273.51, 11633.36, 14510.31]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-24T03:21:14.704Z*
