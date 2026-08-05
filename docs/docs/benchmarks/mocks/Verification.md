---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 715.57 ns | 14.357 ns | 16.534 ns | 3008 B |
| Imposter | 675.66 ns | 10.876 ns | 9.642 ns | 4688 B |
| Mockolate | 392.27 ns | 1.822 ns | 1.615 ns | 2128 B |
| Moq | 344,222.29 ns | 2,049.365 ns | 1,816.709 ns | 24325 B |
| NSubstitute | 6,153.58 ns | 21.623 ns | 19.169 ns | 10064 B |
| FakeItEasy | 7,384.61 ns | 33.335 ns | 31.181 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 413067
  bar [715.57, 675.66, 392.27, 344222.29, 6153.58, 7384.61]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.05 ns | 0.618 ns | 0.578 ns | 320 B |
| Imposter | 322.70 ns | 3.234 ns | 2.866 ns | 2400 B |
| Mockolate | 232.60 ns | 0.998 ns | 0.834 ns | 1144 B |
| Moq | 88,189.86 ns | 488.948 ns | 433.439 ns | 6918 B |
| NSubstitute | 3,468.49 ns | 11.990 ns | 9.361 ns | 7088 B |
| FakeItEasy | 3,714.65 ns | 26.663 ns | 24.940 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 105828
  bar [52.05, 322.7, 232.6, 88189.86, 3468.49, 3714.65]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,205.74 ns | 2.556 ns | 2.134 ns | 4472 B |
| Imposter | 1,658.82 ns | 13.894 ns | 12.317 ns | 11192 B |
| Mockolate | 1,109.96 ns | 5.717 ns | 5.068 ns | 5240 B |
| Moq | 465,768.71 ns | 2,548.548 ns | 2,259.221 ns | 34699 B |
| NSubstitute | 11,421.23 ns | 144.821 ns | 135.466 ns | 16762 B |
| FakeItEasy | 13,124.17 ns | 101.172 ns | 89.686 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 558923
  bar [1205.74, 1658.82, 1109.96, 465768.71, 11421.23, 13124.17]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-05T03:21:19.181Z*
