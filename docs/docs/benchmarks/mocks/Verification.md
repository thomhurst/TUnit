---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-06-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.301
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 787.26 ns | 11.759 ns | 10.999 ns | 3008 B |
| Imposter | 704.81 ns | 13.933 ns | 18.117 ns | 4688 B |
| Mockolate | 437.08 ns | 2.223 ns | 1.971 ns | 2128 B |
| Moq | 246,636.14 ns | 924.739 ns | 772.199 ns | 24336 B |
| NSubstitute | 6,129.63 ns | 52.256 ns | 48.880 ns | 10064 B |
| FakeItEasy | 6,885.88 ns | 47.930 ns | 40.024 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 295964
  bar [787.26, 704.81, 437.08, 246636.14, 6129.63, 6885.88]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 57.41 ns | 0.324 ns | 0.271 ns | 320 B |
| Imposter | 353.35 ns | 7.121 ns | 6.994 ns | 2400 B |
| Mockolate | 254.59 ns | 2.376 ns | 2.222 ns | 1144 B |
| Moq | 62,592.84 ns | 343.484 ns | 268.169 ns | 6925 B |
| NSubstitute | 3,687.13 ns | 33.407 ns | 31.249 ns | 7088 B |
| FakeItEasy | 3,583.40 ns | 57.530 ns | 53.813 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75112
  bar [57.41, 353.35, 254.59, 62592.84, 3687.13, 3583.4]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,368.56 ns | 26.877 ns | 29.874 ns | 4472 B |
| Imposter | 1,778.97 ns | 21.680 ns | 35.620 ns | 11192 B |
| Mockolate | 1,201.10 ns | 15.734 ns | 14.718 ns | 5240 B |
| Moq | 350,319.85 ns | 3,740.030 ns | 3,498.426 ns | 34842 B |
| NSubstitute | 10,652.32 ns | 143.812 ns | 134.521 ns | 16762 B |
| FakeItEasy | 12,219.41 ns | 191.176 ns | 178.826 ns | 19312 B |

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
  y-axis "Time (ns)" 0 --> 420384
  bar [1368.56, 1778.97, 1201.1, 350319.85, 10652.32, 12219.41]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-06-22T03:30:58.892Z*
