---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 719.52 ns | 9.971 ns | 9.327 ns | 3008 B |
| Imposter | 715.70 ns | 14.036 ns | 15.018 ns | 4688 B |
| Mockolate | 410.06 ns | 2.212 ns | 1.961 ns | 2128 B |
| Moq | 349,164.36 ns | 2,270.328 ns | 2,012.587 ns | 24325 B |
| NSubstitute | 6,576.91 ns | 130.954 ns | 145.555 ns | 10064 B |
| FakeItEasy | 7,655.63 ns | 80.318 ns | 75.129 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 418998
  bar [719.52, 715.7, 410.06, 349164.36, 6576.91, 7655.63]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.65 ns | 0.723 ns | 0.604 ns | 320 B |
| Imposter | 333.14 ns | 4.104 ns | 3.839 ns | 2400 B |
| Mockolate | 230.34 ns | 2.365 ns | 2.212 ns | 1144 B |
| Moq | 88,816.24 ns | 497.321 ns | 440.862 ns | 6918 B |
| NSubstitute | 3,582.35 ns | 21.791 ns | 20.383 ns | 7088 B |
| FakeItEasy | 3,683.45 ns | 62.336 ns | 55.259 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106580
  bar [52.65, 333.14, 230.34, 88816.24, 3582.35, 3683.45]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,273.18 ns | 14.169 ns | 13.254 ns | 4472 B |
| Imposter | 1,778.50 ns | 28.451 ns | 26.613 ns | 11192 B |
| Mockolate | 1,145.24 ns | 12.583 ns | 11.771 ns | 5240 B |
| Moq | 474,502.72 ns | 2,145.655 ns | 1,902.067 ns | 34699 B |
| NSubstitute | 11,302.78 ns | 68.972 ns | 64.517 ns | 16762 B |
| FakeItEasy | 13,828.99 ns | 272.873 ns | 280.221 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 569404
  bar [1273.18, 1778.5, 1145.24, 474502.72, 11302.78, 13828.99]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-03T03:22:34.236Z*
