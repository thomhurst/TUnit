---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 726.84 ns | 8.313 ns | 7.369 ns | 3008 B |
| Imposter | 722.49 ns | 12.927 ns | 12.091 ns | 4688 B |
| Mockolate | 412.91 ns | 5.574 ns | 5.214 ns | 2128 B |
| Moq | 347,979.68 ns | 2,614.534 ns | 2,183.254 ns | 24325 B |
| NSubstitute | 6,490.58 ns | 93.337 ns | 87.307 ns | 10176 B |
| FakeItEasy | 7,854.80 ns | 75.234 ns | 70.374 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 417576
  bar [726.84, 722.49, 412.91, 347979.68, 6490.58, 7854.8]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 53.47 ns | 0.879 ns | 0.822 ns | 320 B |
| Imposter | 326.05 ns | 5.103 ns | 4.773 ns | 2400 B |
| Mockolate | 238.57 ns | 3.795 ns | 3.550 ns | 1144 B |
| Moq | 90,636.00 ns | 733.247 ns | 685.879 ns | 6918 B |
| NSubstitute | 3,787.37 ns | 36.938 ns | 32.745 ns | 7088 B |
| FakeItEasy | 3,626.97 ns | 43.786 ns | 38.815 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 108764
  bar [53.47, 326.05, 238.57, 90636, 3787.37, 3626.97]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,301.98 ns | 22.278 ns | 19.749 ns | 4472 B |
| Imposter | 1,785.47 ns | 26.358 ns | 23.366 ns | 11192 B |
| Mockolate | 1,173.10 ns | 23.099 ns | 25.674 ns | 5240 B |
| Moq | 487,190.24 ns | 3,822.985 ns | 3,576.023 ns | 35496 B |
| NSubstitute | 11,586.49 ns | 97.210 ns | 90.930 ns | 16763 B |
| FakeItEasy | 13,591.00 ns | 196.758 ns | 184.047 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 584629
  bar [1301.98, 1785.47, 1173.1, 487190.24, 11586.49, 13591]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-13T03:22:56.594Z*
