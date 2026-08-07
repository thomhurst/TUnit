---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 714.90 ns | 6.785 ns | 6.347 ns | 3008 B |
| Imposter | 685.99 ns | 6.748 ns | 6.312 ns | 4688 B |
| Mockolate | 402.86 ns | 4.773 ns | 4.231 ns | 2128 B |
| Moq | 343,500.97 ns | 2,269.556 ns | 1,895.182 ns | 24325 B |
| NSubstitute | 6,322.38 ns | 49.239 ns | 43.649 ns | 10064 B |
| FakeItEasy | 7,362.33 ns | 46.381 ns | 41.116 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 412202
  bar [714.9, 685.99, 402.86, 343500.97, 6322.38, 7362.33]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.32 ns | 0.438 ns | 0.388 ns | 320 B |
| Imposter | 325.86 ns | 1.710 ns | 1.516 ns | 2400 B |
| Mockolate | 225.32 ns | 1.945 ns | 1.724 ns | 1144 B |
| Moq | 89,450.53 ns | 452.324 ns | 400.973 ns | 6918 B |
| NSubstitute | 3,498.94 ns | 13.694 ns | 12.809 ns | 7088 B |
| FakeItEasy | 3,523.12 ns | 21.313 ns | 17.797 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 107341
  bar [51.32, 325.86, 225.32, 89450.53, 3498.94, 3523.12]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,268.29 ns | 8.431 ns | 7.474 ns | 4472 B |
| Imposter | 1,741.67 ns | 34.679 ns | 37.107 ns | 11192 B |
| Mockolate | 1,106.11 ns | 11.231 ns | 10.506 ns | 5240 B |
| Moq | 475,533.46 ns | 3,180.022 ns | 2,819.007 ns | 34699 B |
| NSubstitute | 11,603.14 ns | 61.730 ns | 51.548 ns | 16762 B |
| FakeItEasy | 13,271.29 ns | 139.424 ns | 123.596 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 570641
  bar [1268.29, 1741.67, 1106.11, 475533.46, 11603.14, 13271.29]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-07T03:18:12.757Z*
