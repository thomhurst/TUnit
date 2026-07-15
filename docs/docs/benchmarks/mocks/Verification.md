---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 946.17 ns | 11.085 ns | 10.369 ns | 3008 B |
| Imposter | 818.68 ns | 15.757 ns | 14.739 ns | 4688 B |
| Mockolate | 490.16 ns | 8.615 ns | 8.058 ns | 2128 B |
| Moq | 353,014.01 ns | 2,981.047 ns | 2,642.620 ns | 24325 B |
| NSubstitute | 6,757.85 ns | 51.412 ns | 48.091 ns | 10176 B |
| FakeItEasy | 7,919.01 ns | 57.833 ns | 51.268 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 423617
  bar [946.17, 818.68, 490.16, 353014.01, 6757.85, 7919.01]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 60.35 ns | 0.915 ns | 0.856 ns | 320 B |
| Imposter | 376.14 ns | 6.737 ns | 6.302 ns | 2400 B |
| Mockolate | 257.43 ns | 4.867 ns | 4.780 ns | 1144 B |
| Moq | 89,654.75 ns | 349.262 ns | 309.612 ns | 6918 B |
| NSubstitute | 3,889.21 ns | 20.009 ns | 18.716 ns | 7088 B |
| FakeItEasy | 3,924.29 ns | 68.132 ns | 63.730 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107586
  bar [60.35, 376.14, 257.43, 89654.75, 3889.21, 3924.29]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,405.81 ns | 25.434 ns | 22.546 ns | 4472 B |
| Imposter | 2,059.12 ns | 37.674 ns | 35.240 ns | 11192 B |
| Mockolate | 1,277.02 ns | 16.888 ns | 15.797 ns | 5240 B |
| Moq | 487,096.27 ns | 2,448.528 ns | 2,290.355 ns | 34699 B |
| NSubstitute | 12,142.82 ns | 46.975 ns | 39.227 ns | 16763 B |
| FakeItEasy | 14,456.70 ns | 267.206 ns | 249.945 ns | 19346 B |

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
  y-axis "Time (ns)" 0 --> 584516
  bar [1405.81, 2059.12, 1277.02, 487096.27, 12142.82, 14456.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-15T03:20:35.055Z*
