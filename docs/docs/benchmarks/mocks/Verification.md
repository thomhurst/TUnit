---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 726.43 ns | 14.392 ns | 15.997 ns | 3008 B |
| Imposter | 709.66 ns | 12.601 ns | 11.170 ns | 4688 B |
| Mockolate | 437.19 ns | 8.769 ns | 9.747 ns | 2240 B |
| Moq | 345,643.09 ns | 2,485.066 ns | 2,075.142 ns | 24325 B |
| NSubstitute | 6,337.24 ns | 61.927 ns | 54.897 ns | 10064 B |
| FakeItEasy | 7,729.30 ns | 67.974 ns | 63.583 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 414772
  bar [726.43, 709.66, 437.19, 345643.09, 6337.24, 7729.3]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.79 ns | 0.426 ns | 0.356 ns | 320 B |
| Imposter | 335.46 ns | 3.877 ns | 3.627 ns | 2400 B |
| Mockolate | 249.80 ns | 3.227 ns | 3.019 ns | 1240 B |
| Moq | 88,476.37 ns | 668.638 ns | 625.445 ns | 6918 B |
| NSubstitute | 3,638.22 ns | 33.695 ns | 29.870 ns | 7088 B |
| FakeItEasy | 3,580.28 ns | 61.219 ns | 57.264 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106172
  bar [52.79, 335.46, 249.8, 88476.37, 3638.22, 3580.28]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,316.24 ns | 15.467 ns | 14.468 ns | 4472 B |
| Imposter | 1,867.10 ns | 31.574 ns | 27.990 ns | 11192 B |
| Mockolate | 1,242.72 ns | 24.213 ns | 22.649 ns | 5376 B |
| Moq | 484,355.68 ns | 2,098.368 ns | 1,860.149 ns | 34779 B |
| NSubstitute | 11,469.88 ns | 68.947 ns | 61.120 ns | 16762 B |
| FakeItEasy | 14,146.45 ns | 233.737 ns | 207.202 ns | 19345 B |

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
  y-axis "Time (ns)" 0 --> 581227
  bar [1316.24, 1867.1, 1242.72, 484355.68, 11469.88, 14146.45]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-14T03:35:08.044Z*
