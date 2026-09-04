---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 996.63 ns | 11.722 ns | 10.965 ns | 3008 B |
| Imposter | 1,029.90 ns | 14.239 ns | 12.622 ns | 4688 B |
| Mockolate | 582.88 ns | 7.604 ns | 7.113 ns | 2128 B |
| Moq | 256,197.99 ns | 1,808.975 ns | 1,603.609 ns | 24306 B |
| NSubstitute | 7,438.36 ns | 48.396 ns | 42.902 ns | 10064 B |
| FakeItEasy | 7,377.24 ns | 43.861 ns | 38.882 ns | 10731 B |

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
  y-axis "Time (ns)" 0 --> 307438
  bar [996.63, 1029.9, 582.88, 256197.99, 7438.36, 7377.24]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 71.58 ns | 1.433 ns | 1.962 ns | 320 B |
| Imposter | 471.49 ns | 6.386 ns | 5.974 ns | 2400 B |
| Mockolate | 316.54 ns | 6.311 ns | 8.638 ns | 1144 B |
| Moq | 67,662.70 ns | 422.716 ns | 374.727 ns | 6925 B |
| NSubstitute | 3,982.31 ns | 27.547 ns | 25.767 ns | 7088 B |
| FakeItEasy | 3,817.50 ns | 34.006 ns | 30.145 ns | 5299 B |

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
  y-axis "Time (ns)" 0 --> 81196
  bar [71.58, 471.49, 316.54, 67662.7, 3982.31, 3817.5]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,638.65 ns | 14.438 ns | 13.506 ns | 4472 B |
| Imposter | 2,347.77 ns | 46.412 ns | 78.812 ns | 11192 B |
| Mockolate | 1,414.07 ns | 24.072 ns | 22.517 ns | 5240 B |
| Moq | 356,518.29 ns | 2,143.733 ns | 1,900.364 ns | 34814 B |
| NSubstitute | 12,792.53 ns | 64.138 ns | 56.857 ns | 16762 B |
| FakeItEasy | 13,248.07 ns | 33.295 ns | 29.516 ns | 19238 B |

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
  y-axis "Time (ns)" 0 --> 427822
  bar [1638.65, 2347.77, 1414.07, 356518.29, 12792.53, 13248.07]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-04T02:33:16.366Z*
