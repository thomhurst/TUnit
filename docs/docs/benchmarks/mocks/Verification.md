---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 646.84 ns | 3.681 ns | 3.263 ns | 2864 B |
| Imposter | 690.73 ns | 8.939 ns | 8.362 ns | 4688 B |
| Mockolate | 421.08 ns | 2.629 ns | 2.331 ns | 2240 B |
| Moq | 342,715.22 ns | 3,134.614 ns | 2,932.120 ns | 24325 B |
| NSubstitute | 6,213.90 ns | 106.649 ns | 99.759 ns | 10064 B |
| FakeItEasy | 7,847.67 ns | 101.987 ns | 90.409 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 411259
  bar [646.84, 690.73, 421.08, 342715.22, 6213.9, 7847.67]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.64 ns | 0.858 ns | 0.803 ns | 304 B |
| Imposter | 364.33 ns | 7.281 ns | 9.208 ns | 2400 B |
| Mockolate | 256.56 ns | 3.556 ns | 3.152 ns | 1240 B |
| Moq | 88,335.35 ns | 264.618 ns | 206.596 ns | 6918 B |
| NSubstitute | 3,692.85 ns | 29.034 ns | 25.738 ns | 7088 B |
| FakeItEasy | 3,704.23 ns | 53.853 ns | 50.374 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106003
  bar [52.64, 364.33, 256.56, 88335.35, 3692.85, 3704.23]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,182.50 ns | 12.125 ns | 10.748 ns | 4176 B |
| Imposter | 1,788.78 ns | 35.446 ns | 47.320 ns | 11192 B |
| Mockolate | 1,145.04 ns | 14.459 ns | 13.525 ns | 5376 B |
| Moq | 467,892.00 ns | 2,203.465 ns | 1,839.993 ns | 35066 B |
| NSubstitute | 10,975.39 ns | 62.548 ns | 55.447 ns | 16762 B |
| FakeItEasy | 13,412.00 ns | 207.310 ns | 173.113 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 561471
  bar [1182.5, 1788.78, 1145.04, 467892, 10975.39, 13412]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-16T03:25:52.400Z*
