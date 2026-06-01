---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-06-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 768.38 ns | 3.768 ns | 3.524 ns | 2968 B |
| Imposter | 728.90 ns | 6.358 ns | 5.947 ns | 4688 B |
| Mockolate | 420.23 ns | 2.128 ns | 1.991 ns | 2240 B |
| Moq | 240,686.23 ns | 850.537 ns | 710.237 ns | 24324 B |
| NSubstitute | 6,177.72 ns | 51.252 ns | 45.434 ns | 10064 B |
| FakeItEasy | 6,876.82 ns | 39.465 ns | 34.985 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 288824
  bar [768.38, 728.9, 420.23, 240686.23, 6177.72, 6876.82]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.12 ns | 0.898 ns | 0.840 ns | 304 B |
| Imposter | 332.99 ns | 4.841 ns | 4.292 ns | 2400 B |
| Mockolate | 255.87 ns | 2.055 ns | 1.822 ns | 1240 B |
| Moq | 63,516.56 ns | 419.555 ns | 371.924 ns | 6925 B |
| NSubstitute | 3,619.76 ns | 16.274 ns | 13.590 ns | 7088 B |
| FakeItEasy | 3,462.95 ns | 38.638 ns | 34.252 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 76220
  bar [52.12, 332.99, 255.87, 63516.56, 3619.76, 3462.95]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,324.59 ns | 15.945 ns | 14.915 ns | 4384 B |
| Imposter | 1,914.04 ns | 25.246 ns | 23.615 ns | 11192 B |
| Mockolate | 1,284.00 ns | 14.553 ns | 13.613 ns | 5376 B |
| Moq | 368,332.11 ns | 2,085.203 ns | 1,848.478 ns | 34699 B |
| NSubstitute | 11,049.85 ns | 74.116 ns | 69.328 ns | 16762 B |
| FakeItEasy | 12,550.44 ns | 119.832 ns | 106.228 ns | 19456 B |

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
  y-axis "Time (ns)" 0 --> 441999
  bar [1324.59, 1914.04, 1284, 368332.11, 11049.85, 12550.44]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-01T03:31:09.013Z*
